using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Production.Api;
using System.Reflection;
using System.Runtime.Serialization;

namespace PlayHouse.Service.Api;
public class ApiMethod
{
    public int MsgId { get; set; }
    public string ClassName { get; set; }
    public MethodInfo Method { get; set; }
    public ApiMethod(int msgId, string className, MethodInfo method)
    {
        MsgId = msgId;
        ClassName = className;
        Method = method;
    }
}

public class ApiInstance
{
    public object Instance { get; set; }
    public MethodInfo Method { get; set; }
    public ApiInstance(object instance,MethodInfo methodInfo)
    {
        Instance = instance;
        Method = methodInfo;
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

    //private Type[] GetAllSubtypes(params Type[] subTypes)
    //{
    //    return AppDomain.CurrentDomain.GetAssemblies()
    //        .SelectMany(assembly => assembly.GetTypes())
    //        .Where(types => types.Any(type => type.IsAssignableFrom(subtype) && subtype != type ))
    //        .ToArray();
    //}

    private Type[] GetAllSubtypes(params Type[] subTypes)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && subTypes.Any(subType => subType.IsAssignableFrom(type)))
            .ToArray();
    }
    public List<(string, Object,MethodInfo)> InvokeMethodByName(string methodName, params object[] arguments)
    {
        return _findTypes
            .SelectMany(type => type.GetMethods())
            .Where(m => m.Name == methodName)
            .Select(m =>
            {
                var instance = FormatterServices.GetUninitializedObject(m.DeclaringType!);
                //var instance = Activator.CreateInstance(m.DeclaringType!);
                //var result = m.Invoke(instance, arguments);
                return (m.DeclaringType!.FullName!, instance!,m);
            })
            .ToList();
    }
}

public class ApiReflection
{
    private readonly Dictionary<string, ApiInstance> _instances = new Dictionary<string, ApiInstance>();
    private readonly Dictionary<int, ApiMethod> _methods = new Dictionary<int, ApiMethod>();
    private readonly Dictionary<int, ApiMethod> _backendMethods = new Dictionary<int, ApiMethod>();
    private readonly Dictionary<int, string> _messageIndexChecker = new Dictionary<int, string>();
    private readonly Dictionary<int, string> _messageIndexBackendChecker = new Dictionary<int, string>();

    public ApiReflection()
    {
        var reflections = new Reflections(typeof(IApiController));
        ExtractInstance(reflections);
        ExtractHandlerMethod(reflections);
    }


    public async Task CallMethod(RouteHeader routeHeader, Packet packet, IApiSender apiSender)
    {
        var msgId = routeHeader.MsgId;
        var targetMethod = _methods.ContainsKey(msgId) ? _methods[msgId] : null;
        if (targetMethod == null) throw new ApiException.NotRegisterApiMethod($"not registered message msgId:{msgId}");

        if (!_instances.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
        var classInstance = _instances[targetMethod.ClassName];

        var targetInstance = classInstance.Method.Invoke(classInstance.Instance,null);
        var task = (Task)targetMethod.Method.Invoke(targetInstance, new object[] { packet, apiSender })!;
        await task;
    }

    public async Task BackendCallMethod(RouteHeader routeHeader, Packet packet,IApiBackendSender apiBackendSender)
    {
        var msgId = routeHeader.MsgId;
        var targetMethod = _backendMethods.ContainsKey(msgId) ? _backendMethods[msgId] : null;
        if (targetMethod == null) throw new ApiException.NotRegisterApiMethod($"not registered message msgId:{msgId}");

        if (!_instances.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
        var classInstance = _instances[targetMethod.ClassName];

        var targetInstance = classInstance.Method.Invoke(classInstance.Instance, null);
        var task = (Task)targetMethod.Method.Invoke(targetInstance, new object[] { packet, apiBackendSender})!;
        await task;
    }

    private void ExtractInstance(Reflections reflections)
    {

        reflections.InvokeMethodByName("Instance").ForEach(instance => _instances.Add(instance.Item1, new ApiInstance(instance.Item2,instance.Item3)));
    }

    private void ExtractHandlerMethod(Reflections reflections)
    {
        //RegisterInitMethod(reflections);
        RegisterHandlerMethod(reflections);
    }

    //private void RegisterInitMethod(Reflections reflections)
    //{
    //    //var apiServiceReflections = new Reflections(typeof(IApiService));
    //    reflections.GetMethodsBySignature("Init", typeof(Task), typeof(ISystemPanel), typeof(ISender)).ForEach(el => {
    //        _initMethods.Add(new ApiMethod(0, el.DeclaringType!.FullName!, el));
    //    });

    //}

    private void RegisterHandlerMethod(Reflections reflections)
    {
        //var reflections = new Reflections(typeof(IApiService));
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
                _methods[key] = new ApiMethod(key, className, value.Method);
                _messageIndexChecker[key] = value.GetMethodInfo().Name;
            }

            foreach (var (key, value) in backendHandlerRegister.Handles)
            {
                if (_messageIndexBackendChecker.ContainsKey(key))
                {
                    throw new ApiException($"registered msgId is duplicated - msgId:{key}, methods: {_messageIndexChecker[key]}, {value.GetMethodInfo().Name}");
                }
                _backendMethods[key] = new ApiMethod(key, className, value.Method);
                _messageIndexBackendChecker[key] = value.GetMethodInfo().Name;
            }

        });

    }
    //private void RegisterBackendHandlerMethod(Reflections reflections)
    //{
    //    //var reflections = new Reflections(typeof(IApiBackendService));
    //    reflections.GetMethodsBySignature("Handles", typeof(void), typeof(IBackendHandlerRegister)).ForEach(methodInfo =>
    //    {
    //        var className = methodInfo.DeclaringType!.FullName!;
    //        var apiInstance = _instances[className!]!;
    //        var handlerRegister = new XBackendHandlerRegister();
    //        methodInfo.Invoke(apiInstance.Instance, new object[] { handlerRegister });


    //        foreach (var (key, value) in handlerRegister.Handles)
    //        {
    //            if (_messageIndexBackendChecker.ContainsKey(key))
    //            {
    //                throw new ApiException($"registered msgId is duplicated - msgId:{key}, methods: {_messageIndexChecker[key]}, {value.GetMethodInfo().Name}");
    //            }
    //            _backendMethods[key] = new ApiMethod(key, className, value.Method);
    //            _messageIndexBackendChecker[key] = value.GetMethodInfo().Name;
    //        }

    //    });

    //}


}
