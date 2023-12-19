using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Production.Api;
using PlayHouse.Production.Api.Filter;
using System.Reflection;
using System.Runtime.Serialization;

namespace PlayHouse.Service.Api;
public class ApiMethod
{
    public int MsgId { get; set; }
    public string ClassName { get; set; }
    public MethodInfo Method { get; set; }

    public IEnumerable<ApiActionFilterAttribute> Filters { get; set; }
    public ApiMethod(int msgId, string className, MethodInfo method)
    {
        MsgId = msgId;
        ClassName = className;
        Method = method;
        Filters = method.GetCustomAttributes(typeof(ApiActionFilterAttribute), true)
            .Select(e => (ApiActionFilterAttribute)e);
            
        
    }
}

public class BackendApiMethod
{
    public int MsgId { get; set; }
    public string ClassName { get; set; }
    public MethodInfo Method { get; set; }

    public IEnumerable<ApiBackendActionFilterAttribute> Filters { get; set; }
    public BackendApiMethod(int msgId, string className, MethodInfo method)
    {
        MsgId = msgId;
        ClassName = className;
        Method = method;
        Filters = method.GetCustomAttributes(typeof(ApiBackendActionFilterAttribute), true)
            .Select(e => (ApiBackendActionFilterAttribute)e);
    }
}

public class ApiInstance
{
    public object Instance { get; set; }
    public MethodInfo Method { get; set; }

    public IEnumerable<ApiActionFilterAttribute> ApiFilters { get; set; }
    public IEnumerable<ApiBackendActionFilterAttribute> BackendApiFilter { get; set; }

    public ApiInstance(object instance, MethodInfo methodInfo, IEnumerable<ApiActionFilterAttribute> apiFilters, IEnumerable<ApiBackendActionFilterAttribute> backendApiFilter)
    {
        Instance = instance;
        Method = methodInfo;
        ApiFilters = apiFilters;
        BackendApiFilter = backendApiFilter;
    }

    internal async Task Invoke(ApiMethod targetMethod, Packet packet, IApiSender apiSender)
    {
        var targetInstance = Method.Invoke(Instance, null);

        //global before filter
        foreach(ApiActionFilterAttribute filter in GlobalApiActionManager.GetFilters())
        {
            if (filter == null) continue;
            filter.BeforeExecution(packet, apiSender);
        }

        //class before filter
        foreach (ApiActionFilterAttribute filter in ApiFilters)
        {
            if (filter == null) continue;
            filter.BeforeExecution(packet, apiSender);
        }


        //method before filter
        foreach (ApiActionFilterAttribute filter in targetMethod.Filters)
        {
            if (filter == null) continue;
            filter.BeforeExecution(packet, apiSender);
        }


        var task = (Task)targetMethod.Method.Invoke(targetInstance, new object[] { packet, apiSender })!;
        await task;

        //method after filter
        foreach (ApiActionFilterAttribute filter in targetMethod.Filters.Reverse())
        {
            if (filter == null) continue;
            filter.AfterExecution(packet, apiSender);
        }

        //class after filter
        foreach (ApiActionFilterAttribute filter in ApiFilters.Reverse())
        {
            if (filter == null) continue;
            filter.AfterExecution(packet, apiSender);
        }

        //global after filter
        foreach (ApiActionFilterAttribute filter in GlobalApiActionManager.GetFilters().Reverse())
        {
            if (filter == null) continue;
            filter.AfterExecution(packet, apiSender);
        }

    }

    internal async Task Invoke(BackendApiMethod targetMethod, Packet packet, IApiBackendSender apiSender)
    {
        var targetInstance = Method.Invoke(Instance, null);

        //global before filter
        foreach (IApiBackendFilter filter in GlobalBackendApiActionManager.GetFilters())
        {
            if (filter == null) continue;
            filter.BeforeExecution(packet, apiSender);
        }

        //class before filter
        foreach (IApiBackendFilter filter in BackendApiFilter)
        {
            if (filter == null) continue;
            filter.BeforeExecution(packet, apiSender);
        }


        //method before filter
        foreach (IApiBackendFilter filter in targetMethod.Filters)
        {
            if (filter == null) continue;
            filter.BeforeExecution(packet, apiSender);
        }


        var task = (Task)targetMethod.Method.Invoke(targetInstance, new object[] { packet, apiSender })!;
        await task;

        //method after filter
        foreach (IApiBackendFilter filter in targetMethod.Filters.Reverse())
        {
            if (filter == null) continue;
            filter.AfterExecution(packet, apiSender);
        }

        //class after filter
        foreach (IApiBackendFilter filter in BackendApiFilter.Reverse())
        {
            if (filter == null) continue;
            filter.AfterExecution(packet, apiSender);
        }

        //global IBackendFilter filter
        foreach (IApiBackendFilter filter in GlobalBackendApiActionManager.GetFilters().Reverse())
        {
            if (filter == null) continue;
            filter.AfterExecution(packet, apiSender);
        }

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
                IEnumerable<ApiActionFilterAttribute> apiFilters = m.DeclaringType!.GetCustomAttributes(typeof(ApiActionFilterAttribute), true).Select(e => (ApiActionFilterAttribute)e);
                IEnumerable<ApiBackendActionFilterAttribute> backendApiFilters = m.DeclaringType!.GetCustomAttributes(typeof(ApiBackendActionFilterAttribute), true).Select(e=>(ApiBackendActionFilterAttribute)e);
                //var instance = Activator.CreateInstance(m.DeclaringType!);
                //var result = m.Invoke(instance, arguments);
                //return (m.DeclaringType!.FullName!, instance!,m,apiFilters,backendApiFilters);
                return (m.DeclaringType!.FullName!,new ApiInstance(instance!, m, apiFilters,backendApiFilters));
            })
            .ToList();
    }
}

public class ApiReflection
{
    private readonly Dictionary<string, ApiInstance> _instances = new ();
    private readonly Dictionary<int, ApiMethod> _methods = new ();
    private readonly Dictionary<int, BackendApiMethod> _backendMethods = new ();
    private readonly Dictionary<int, string> _messageIndexChecker = new ();
    private readonly Dictionary<int, string> _messageIndexBackendChecker = new ();

    public ApiReflection()
    {
        var reflections = new Reflections(typeof(IApiController));
        ExtractInstance(reflections);
        ExtractHandlerMethod(reflections);
    }


    public async Task CallMethod(RouteHeader routeHeader, Packet packet, IApiSender apiSender)
    {
        int msgId = routeHeader.MsgId;
        ApiMethod? targetMethod = _methods.TryGetValue(msgId, value: out var method) ? method : null;
        if (targetMethod == null) throw new ApiException.NotRegisterApiMethod($"not registered message msgId:{msgId}");

        if (!_instances.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
        ApiInstance classInstance = _instances[targetMethod.ClassName];

        await classInstance.Invoke(targetMethod,packet,apiSender);
        

        //var targetInstance = classInstance.Method.Invoke(classInstance.Instance, null);
        //var task = (Task)targetMethod.Method.Invoke(targetInstance, new object[] { packet, apiSender })!;
        //await task;
    }

    public async Task BackendCallMethod(RouteHeader routeHeader, Packet packet,IApiBackendSender apiBackendSender)
    {
        int msgId = routeHeader.MsgId;
        BackendApiMethod? targetMethod = _backendMethods.TryGetValue(msgId, out var method) ? method : null;
        if (targetMethod == null) throw new ApiException.NotRegisterApiMethod($"not registered message msgId:{msgId}");

        if (!_instances.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
        ApiInstance classInstance = _instances[targetMethod.ClassName];

        await classInstance.Invoke(targetMethod, packet, apiBackendSender);

        //var targetInstance = classInstance.Method.Invoke(classInstance.Instance, null);
        //var task = (Task)targetMethod.Method.Invoke(targetInstance, new object[] { packet, apiBackendSender})!;
        //await task;
    }

    private void ExtractInstance(Reflections reflections)
    {

        reflections.InvokeMethodByName("Instance").ForEach(instance => _instances.Add(instance.instanceName, instance.apiInstance));
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
                _backendMethods[key] = new BackendApiMethod(key, className, value.Method);
                _messageIndexBackendChecker[key] = value.GetMethodInfo().Name;
            }

        });

    }

}
