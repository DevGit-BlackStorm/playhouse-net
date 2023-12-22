using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Production.Api;
using PlayHouse.Production.Api.Aspectify;
using System.Reflection;
using System.Runtime.Serialization;

namespace PlayHouse.Service.Api;
public class ApiMethod
{
    public int MsgId { get; set; }
    public string ClassName { get; set; }
    public MethodInfo Method { get; set; }

    public List<AspectifyAttribute> Filters { get; set; } = new();
    public ApiMethod(int msgId, string className, MethodInfo method, IEnumerable<AspectifyAttribute> classFilters)
    {
        MsgId = msgId;
        ClassName = className;
        Method = method;
        Filters.AddRange(classFilters);
        Filters.AddRange(method.GetCustomAttributes(typeof(AspectifyAttribute), true)
            .Select(e => (AspectifyAttribute)e));
    }
}

//public class BackendApiMethod
//{
//    public int MsgId { get; set; }
//    public string ClassName { get; set; }
//    public MethodInfo Method { get; set; }

//    public IEnumerable<ApiBackendActionFilterAttribute> Filters { get; set; }
//    public BackendApiMethod(int msgId, string className, MethodInfo method)
//    {
//        MsgId = msgId;
//        ClassName = className;
//        Method = method;
//        Filters = method.GetCustomAttributes(typeof(ApiBackendActionFilterAttribute), true)
//            .Select(e => (ApiBackendActionFilterAttribute)e);
//    }
//}

public class ApiInstance
{
    public object Instance { get; set; }
    public MethodInfo Method { get; set; }

    public IEnumerable<AspectifyAttribute> ApiFilters { get; set; }
    ///public IEnumerable<ApiBackendActionFilterAttribute> BackendApiFilter { get; set; }

    public ApiInstance(object instance, MethodInfo methodInfo, IEnumerable<AspectifyAttribute> apiFilters)
    {
        Instance = instance;
        Method = methodInfo;
        ApiFilters = apiFilters;
    }

    internal async Task Invoke(ApiMethod targetMethod, IPacket packet, IApiSender apiSender)
    {
        var targetInstance = (Method.Invoke(Instance, null))!;

        Invocation invocation = new Invocation(targetInstance, targetMethod.Method, new object[] { packet, apiSender }, targetMethod.Filters) ;
        await invocation.Proceed();
    }

    internal async Task Invoke(ApiMethod targetMethod, IPacket packet, IApiBackendSender apiSender)
    {
        var targetInstance = (Method.Invoke(Instance, null))!;

        Invocation invocation = new Invocation(targetInstance, targetMethod.Method, new object[] { packet, apiSender }, targetMethod.Filters);
        await invocation.Proceed();

    }

}


class Reflections
{
    private Type[] _findTypes;

    public Reflections(params Type[] types)
    {
        this._findTypes = GetAllSubtypes(types);
    }

    public List<MethodInfo> GetMethodsBySignature(string methodName, Type returnType, params Type[] parameterTypes)
    {
        return _findTypes
            .SelectMany(type => type.GetMethods())
            .Where(method =>
                method.Name == methodName &&
                method.ReturnType == returnType &&
                method.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
            .ToList();
    }

    private Type[] GetAllSubtypes(params Type[] subTypes)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && subTypes.Any(subType => subType.IsAssignableFrom(type)))
            .ToArray();
    }
    public List<(string instanceName, ApiInstance apiInstance)> InvokeMethodByName(string methodName, params object[] arguments)
    {
        return _findTypes
            .SelectMany(type => type.GetMethods())
            .Where(m => m.Name == methodName)
            .Select(m =>
            {
                var instance = FormatterServices.GetUninitializedObject(m.DeclaringType!);
                IEnumerable<AspectifyAttribute> apiFilters = m.DeclaringType!.GetCustomAttributes(typeof(AspectifyAttribute), true).Select(e => (AspectifyAttribute)e);
                return (m.DeclaringType!.FullName!,new ApiInstance(instance!, m, apiFilters));
            })
            .ToList();
    }
}

internal class ApiReflection
{
    private readonly Dictionary<string, ApiInstance> _instances = new ();
    private readonly Dictionary<int, ApiMethod> _methods = new ();
    private readonly Dictionary<int, ApiMethod> _backendMethods = new ();
    private readonly Dictionary<int, string> _messageIndexChecker = new ();
    private readonly Dictionary<int, string> _messageIndexBackendChecker = new ();

    public ApiReflection()
    {
        var reflections = new Reflections(typeof(IApiController));
        ExtractInstance(reflections);
        ExtractHandlerMethod(reflections);

    }


    public async Task CallMethod(RouteHeader routeHeader, RoutePacket packet, IApiSender apiSender)
    {
        using(packet)
        {
            int msgId = routeHeader.MsgId;
            ApiMethod? targetMethod = _methods.TryGetValue(msgId, value: out var method) ? method : null;
            if (targetMethod == null) throw new ApiException.NotRegisterApiMethod($"not registered message msgId:{msgId}");

            if (!_instances.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
            ApiInstance classInstance = _instances[targetMethod.ClassName];

            await classInstance.Invoke(targetMethod, packet.ToContentsPacket(), apiSender);
        }

    }

    public async Task BackendCallMethod(RouteHeader routeHeader, RoutePacket packet, IApiBackendSender apiBackendSender)
    {
        int msgId = routeHeader.MsgId;
        ApiMethod? targetMethod = _backendMethods.TryGetValue(msgId, out var method) ? method : null;
        if (targetMethod == null) throw new ApiException.NotRegisterApiMethod($"not registered message msgId:{msgId}");

        if (!_instances.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
        ApiInstance classInstance = _instances[targetMethod.ClassName];

        await classInstance.Invoke(targetMethod, packet.ToContentsPacket(), apiBackendSender);
    }


    private void ExtractInstance(Reflections reflections)
    {

        reflections.InvokeMethodByName("Instance").ForEach(instance => _instances.Add(instance.instanceName, instance.apiInstance));
    }

    private void ExtractHandlerMethod(Reflections reflections)
    {
        RegisterHandlerMethod(reflections);
    }


    private void RegisterHandlerMethod(Reflections reflections)
    {
        reflections.GetMethodsBySignature("Handles", typeof(void), typeof(IHandlerRegister), typeof(IBackendHandlerRegister)).ForEach(methodInfo =>
        {
            var className = methodInfo.DeclaringType!.FullName!;
            var apiInstance = _instances[className!]!;
            var handlerRegister = new XHandlerRegister();
            var backendHandlerRegister = new XBackendHandlerRegister();
            methodInfo.Invoke(apiInstance.Instance, new object[] { handlerRegister, backendHandlerRegister });


            foreach (var (key, value) in handlerRegister.Handles)
            {
                if (_messageIndexChecker.ContainsKey(key))
                {
                    throw new ApiException($"registered msgId is duplicated - msgId:{key}, methods: {_messageIndexChecker[key]}, {value.GetMethodInfo().Name}");
                }
                _methods[key] = new ApiMethod(key, className, value.Method, apiInstance.ApiFilters);
                _messageIndexChecker[key] = value.GetMethodInfo().Name;
            }

            foreach (var (key, value) in backendHandlerRegister.Handles)
            {
                if (_messageIndexBackendChecker.ContainsKey(key))
                {
                    throw new ApiException($"registered msgId is duplicated - msgId:{key}, methods: {_messageIndexChecker[key]}, {value.GetMethodInfo().Name}");
                }
                _backendMethods[key] = new ApiMethod(key, className, value.Method, apiInstance.ApiFilters);
                _messageIndexBackendChecker[key] = value.GetMethodInfo().Name;
            }

        });

    }

}
