using Microsoft.Extensions.DependencyInjection;
using PlayHouse.Production.Api.Aspectify;
using PlayHouse.Production.Shared;
using System.Reflection;

namespace PlayHouse.Service.Shared.Reflection;

public class ReflectionMethod
{
    public int MsgId { get; set; }
    public string ClassName { get; set; }
    public MethodInfo Method { get; set; }
    public List<AspectifyAttribute> Filters { get; set; } = new();
    public ReflectionMethod(int msgId, string className, MethodInfo method, IEnumerable<AspectifyAttribute> targetFilters, IEnumerable<AspectifyAttribute> classFilters)
    {
        MsgId = msgId;
        ClassName = className;
        Method = method;
        Filters.AddRange(targetFilters);
        Filters.AddRange(classFilters);
        Filters.AddRange(method.GetCustomAttributes(typeof(AspectifyAttribute), true)
            .Select(e => (AspectifyAttribute)e));
    }

    public ReflectionMethod(int msgId, string className, MethodInfo method)
    {
        MsgId = msgId;
        ClassName = className;
        Method = method;
        Filters.AddRange(method.GetCustomAttributes(typeof(AspectifyAttribute), true)
         .Select(e => (AspectifyAttribute)e));
    }
}

public class ReflectionInstance
{
    public object Instance { get; set; }
    public Type Type { get; set; }

    public IEnumerable<AspectifyAttribute> Filters { get; set; }
    public IServiceProvider ServiceProvider { get; set; }

    public string Name => Type.FullName!;

    public ReflectionInstance(Type type, IEnumerable<AspectifyAttribute> filters, IServiceProvider serviceProvider)
    {
        Type = type;
        //Instance = FormatterServices.GetUninitializedObject(type);
        //Instance = Activator.CreateInstance(type)!;
        Instance = ActivatorUtilities.CreateInstance(serviceProvider, type);
        Filters = filters;
        ServiceProvider = serviceProvider;
    }

    internal async Task Invoke(ReflectionMethod targetMethod,params object[] arguements)
    {
        using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var targetInstance = scope.ServiceProvider.GetRequiredService(Type);
            Invocation invocation = new Invocation(targetInstance, targetMethod.Method, arguements, targetMethod.Filters, ServiceProvider);
            await invocation.Proceed();
        }
    }

    internal async Task<object> InvokeWithReturn(ReflectionMethod targetMethod, object[] arguements)
    {
        using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var targetInstance = scope.ServiceProvider.GetRequiredService(Type);
            Invocation invocation = new Invocation(targetInstance, targetMethod.Method, arguements, targetMethod.Filters, ServiceProvider);
            await invocation.Proceed();
            return invocation.ReturnValue!;
        }
    }
}

class ReflectionOperator
{
    private Type[] _findTypes;
    private IServiceProvider _serviceProvider;

    public ReflectionOperator(IServiceProvider serviceProvider, params Type[] types)
    {
        _serviceProvider = serviceProvider;
        _findTypes = GetAllSubtypes(types);

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

    public List<ReflectionInstance> GetInstanceBy(params Type[] targetTypes)
    {
        return _findTypes
            .Where(type => type.IsClass && !type.IsAbstract && targetTypes.Any(targetType => targetType.IsAssignableFrom(type)))
            .Select(type =>
            {
                IEnumerable<AspectifyAttribute> apiFilters = type.GetCustomAttributes(typeof(AspectifyAttribute), true).Select(e => (AspectifyAttribute)e);
                return new ReflectionInstance(type, apiFilters, _serviceProvider);
            }).ToList();
    }

    //public List<ReflectionInstance> GetInstanceBy(Type targetType)
    //{
    //    return _findTypes
    //        .Where(type => type.IsClass && !type.IsAbstract && targetType.IsAssignableFrom(type))
    //        .Select(type =>
    //        {
    //            IEnumerable<AspectifyAttribute> apiFilters = type.GetCustomAttributes(typeof(AspectifyAttribute), true).Select(e => (AspectifyAttribute)e);
    //            return new ReflectionInstance(type, apiFilters, _serviceProvider);
    //        }).ToList();
    //}

    internal List<MethodInfo> GetMethodsBy(Type targetType)
    {
        List<string> methods = targetType.GetMethods().Select(e => e.Name).ToList();

        return _findTypes
            .Where(type => type.IsClass && !type.IsAbstract && targetType.IsAssignableFrom(type))
            .SelectMany(type => type.GetMethods())
            .Where(typeMethod => methods.Contains(typeMethod.Name) && typeMethod.IsPublic).ToList();
    }

}


internal class SystemHandleReflectionInvoker
{
    private readonly Dictionary<string, ReflectionInstance> _instances = new();
    private readonly Dictionary<int, ReflectionMethod> _methods = new();
    private readonly Dictionary<int, string> _messageIndexChecker = new();
    private readonly IEnumerable<AspectifyAttribute> _targetFilters;

    public SystemHandleReflectionInvoker(IServiceProvider serviceProvider, IEnumerable<AspectifyAttribute> targetFilters)
    {
        Type type = typeof(ISystemController);
        _targetFilters = targetFilters;
        var reflections = new ReflectionOperator(serviceProvider, type);
        _instances = reflections.GetInstanceBy(type).ToDictionary(e => e.Name);
        ExtractMethodInfo(reflections);
    }

    public void ExtractMethodInfo(ReflectionOperator reflections)
    {
        reflections.GetMethodsBySignature("Handles", typeof(void), typeof(ISystemHandlerRegister)).ForEach(methodInfo =>
        {
            var className = methodInfo.DeclaringType!.FullName!;
            var systemInstance = _instances[className!]!;
            var handlerRegister = new SystemHandlerRegister();
            
            methodInfo.Invoke(systemInstance.Instance, new object[] { handlerRegister });

            foreach (var (key, value) in handlerRegister.Handles)
            {
                if (_messageIndexChecker.ContainsKey(key))
                {
                    throw new Exception($"registered methodName is duplicated - methodName:{key}, methods: {_messageIndexChecker[key]}, {value.GetMethodInfo().Name}");
                }
                _methods[key] = new ReflectionMethod(key, className, value.Method, _targetFilters, systemInstance.Filters);
                _messageIndexChecker[key] = value.GetMethodInfo().Name;
            }
        });
    }

    public async Task InvokeMethods(int msgId, object[] arguements)
    {
        var method = _methods[msgId];

        if (method == null) throw new ServiceException.NotRegisterMethod($"not registered message methodName:{msgId}");
        if (!_instances.ContainsKey(method.ClassName)) throw new ServiceException.NotRegisterInstance(($"{method.ClassName}: reflection instance is not registered"));

        var instance = _instances[method.ClassName];
        await instance.Invoke(method, arguements);

    }
}


internal class CallbackReflectionInvoker
{
    private readonly Dictionary<string, ReflectionInstance> _instances = new();
    private readonly Dictionary<string, ReflectionMethod> _methods = new();

    public CallbackReflectionInvoker(IServiceProvider serviceProvider, Type[] types)
    {
        var reflections = new ReflectionOperator(serviceProvider, types);

        foreach (var type in types)
        {
            foreach (var instance in reflections.GetInstanceBy(type))
            {
                _instances.Add(instance.Name, instance);
            };
        }

        ExtractMethodInfo(reflections, types);
    }

    private void ExtractMethodInfo(ReflectionOperator reflections, Type[] types)
    {
        foreach (var type in types)
        {

            reflections.GetMethodsBy(type).ForEach(methodInfo =>
            {
                var className = methodInfo.DeclaringType!.FullName!;
                var systemInstance = _instances[className!]!;

                if (_methods.ContainsKey(methodInfo.Name) == false)
                {
                    _methods[methodInfo.Name] = new ReflectionMethod(0, className, methodInfo);
                }
                else
                {
                    throw new Exception("Only one class implementing IDisconnectCallback can exist");
                }
            });
        }
    }

    public async Task InvokeMethods(string methodName, object[] arguements)
    {
        var method = _methods[methodName];

        if (method != null)
        {
            var instance = _instances[method.ClassName];
            await instance.Invoke(method, arguements);
        }
    }

    public async Task<object?> InvokeMethodsWithReturn(string methodName, object[] arguements)
    {
        var method = _methods[methodName];
        var instance = _instances[method.ClassName];
        return await instance.InvokeWithReturn(method, arguements);
    }
}