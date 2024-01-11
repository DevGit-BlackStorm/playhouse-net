using PlayHouse.Production.Api.Aspectify;
using PlayHouse.Service.Shared.Reflection;
using PlayHouse.Service.Shared;
using PlayHouse.Production.Api;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Api.Reflection;

internal class ApiHandleReflectionInvoker
{
    private readonly Dictionary<string, ReflectionInstance> _instances = new();
    private readonly Dictionary<int, ReflectionMethod> _methods = new();
    private readonly Dictionary<int, ReflectionMethod> _backendMethods = new();

    private readonly Dictionary<int, string> _messageIndexChecker = new();
    private readonly Dictionary<int, string> _backendMessageIndexChecker = new();
    private readonly IEnumerable<AspectifyAttribute> _targetFilters;

    public ApiHandleReflectionInvoker(IServiceProvider serviceProvider, IEnumerable<AspectifyAttribute> targetFilters)
    {
        _targetFilters = targetFilters;
        Type type = typeof(IApiController);
        var reflections = new ReflectionOperator(serviceProvider, type);
        _instances = reflections.GetInstanceBy(type).ToDictionary(e => e.Name);
        ExtractMethodInfo(reflections);
    }

    public void ExtractMethodInfo(ReflectionOperator reflections)
    {
        reflections.GetMethodsBySignature("Handles", typeof(void), typeof(IHandlerRegister), typeof(IBackendHandlerRegister)).ForEach(methodInfo =>
        {
            var className = methodInfo.DeclaringType!.FullName!;
            var systemInstance = _instances[className!]!;
            var handlerRegister = new HandlerRegister();
            var backendHandlerRegister = new BackendHandlerRegister();
            methodInfo.Invoke(systemInstance.Instance, new object[] { handlerRegister, backendHandlerRegister });

            foreach (var (key, value) in handlerRegister.Handles)
            {
                if (_messageIndexChecker.ContainsKey(key))
                {
                    throw new Exception($"registered msgId is duplicated - [msgId:{key}, methods: {_messageIndexChecker[key]}, {value.Method.Name}]");
                }
                _methods[key] = new ReflectionMethod(key, className, value.Method, _targetFilters, systemInstance.Filters);
                _messageIndexChecker[key] = value.Method.Name;
            }

            foreach (var (key, value) in backendHandlerRegister.Handles)
            {
                if (_backendMessageIndexChecker.ContainsKey(key))
                {
                    throw new Exception($"registered msgId is duplicated - [msgId:{key}, methods: {_messageIndexChecker[key]}, {value.Method.Name}]");
                }
                _backendMethods[key] = new ReflectionMethod(key, className, value.Method, _targetFilters, systemInstance.Filters);
                _backendMessageIndexChecker[key] = value.Method.Name;
            }
        });
    }

    public async Task InvokeMethods(int msgId, IPacket packet,IApiSender apiSender)
    {

        if(_methods.TryGetValue(msgId,out var method) == false)
        {
            throw new ServiceException.NotRegisterMethod($"not registered message msgId:{msgId}");
        }

        if(_instances.TryGetValue(method.ClassName,out var instance) == false)
        {
            throw new ServiceException.NotRegisterInstance(($"{method.ClassName}: reflection instance is not registered"));
        }
        await instance.Invoke(method, packet,apiSender);
    }

    public async Task InvokeBackendMethods(int msgId, IPacket packet, IApiBackendSender apiSender)
    {

        if (_backendMethods.TryGetValue(msgId, out var method) == false)
        {
            throw new ServiceException.NotRegisterMethod($"not registered message msgId:{msgId}");
        }

        if (_instances.TryGetValue(method.ClassName, out var instance) == false)
        {
            throw new ServiceException.NotRegisterInstance(($"{method.ClassName}: reflection instance is not registered"));
        }
        await instance.Invoke(method, packet, apiSender);
    }
}

