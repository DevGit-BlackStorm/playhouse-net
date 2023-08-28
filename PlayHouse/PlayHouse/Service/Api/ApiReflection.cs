
using Microsoft.Extensions.DependencyInjection;
using Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Production.Api;
using System.Reflection;

namespace PlayHouse.Service.Api
{
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

    //public class ApiInstance
    //{
    //    public object Instance { get; set; }
    //    public ApiInstance(object instance)
    //    {
    //        Instance = instance;
    //    }
    //}

  

    class Reflections
    {
        private Type[] _findTypes;
        
        public Reflections(params Type[] types)
        {
            this._findTypes = GetAllSubtypes(types);
        }


        public List<MethodInfo> GetMethodsBySignature(string methodName,Type returnType, params Type[] parameterTypes)
        {
            return _findTypes
                .SelectMany(type => type.GetMethods())
                .Where(method => 
                    method.Name == methodName &&
                    method.ReturnType == returnType &&
                    method.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
                .ToList();
        }

        public Type[] GetTypes() => _findTypes;
        
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
                    .Where(assembly => !assembly.FullName!.StartsWith("Castle.Core", StringComparison.OrdinalIgnoreCase))  // Castle.Core 어셈블리 제외
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.IsClass &&
                                   (string.IsNullOrEmpty(type.Namespace) ||
                                    !type.Namespace.StartsWith("Castle.Proxies", StringComparison.OrdinalIgnoreCase)) &&  // Castle.Proxies 네임스페이스가 없거나 아닌 경우만 포함
                                   !type.IsAbstract &&
                                   subTypes.Any(subType => subType.IsAssignableFrom(type)))
                    .ToArray();

            //return AppDomain.CurrentDomain.GetAssemblies()
            //    .SelectMany(assembly => assembly.GetTypes())
            //    .Where(type => type.IsClass && 
            //    !type.IsAbstract && subTypes.Any(subType => subType.IsAssignableFrom(type)))
            //    .ToArray();
        }
        public List<(string,Object)> InvokeMethodByName(string methodName, params object[] arguments)
        {
            return _findTypes
                .SelectMany(type => type.GetMethods())
                .Where(m => m.Name == methodName)
                .Select(m =>
                {
                    var instance = Activator.CreateInstance(m.DeclaringType!);
                    var result = m.Invoke(instance, arguments);
                    return  (m.DeclaringType!.FullName!, result!) ;
                })
                .ToList();
        }
    }

    public class ApiReflection
    {
        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();
        //private readonly List<ApiMethod> _initMethods = new List<ApiMethod>();
        private readonly Dictionary<int, ApiMethod> _methods = new Dictionary<int, ApiMethod>();
        private readonly Dictionary<int, ApiMethod> _backendMethods = new Dictionary<int, ApiMethod>();
        private readonly Dictionary<int, string> _messageIndexChecker = new Dictionary<int, string>();
        private readonly Dictionary<int, string> _messageIndexBackendChecker = new Dictionary<int, string>();

        public ApiReflection()
        {
            var reflections = new Reflections(typeof(IApiController));            
            ExtractTypes(reflections);
            ExtractHandlerMethod(reflections);
        }

        //public async Task CallInitMethod(ISystemPanel systemPanel, ISender sender)
        //{
        //    foreach (var targetMethod in _initMethods)
        //    {
        //        try
        //        {
        //            if (!_types.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
        //            var apiInstance = _types[targetMethod.ClassName];
        //            var task =  (Task)targetMethod.Method.Invoke(apiInstance.Instance, new object[] { systemPanel, sender })!;
        //            await task;
        //        }
        //        catch (Exception e)
        //        {
        //            LOG.Error(e.StackTrace, this.GetType(), e);
        //            Environment.Exit(1);
        //        }
        //    }
        //}

        public async Task CallMethod(RouteHeader routeHeader, Packet packet, bool isBackend, AllApiSender apiSender)
        {
            var msgId = routeHeader.MsgId;
            var targetMethod = isBackend ? (_backendMethods.ContainsKey(msgId) ? _backendMethods[msgId] : null) : (_methods.ContainsKey(msgId) ? _methods[msgId] : null);
            if (targetMethod == null) throw new ApiException.NotRegisterApiMethod($"not registered message msgId:{msgId}");

            if (!_types.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
            var type = _types[targetMethod.ClassName];

            try
            {
                var instance = XServiceProvider.Instance.GetRequiredService(type);
                if (isBackend)
                {
                    var task = (Task)targetMethod.Method.Invoke(instance, new object[] { packet, apiSender as IApiBackendSender })!;
                    await task;
                }
                else
                {
                    var task = (Task)targetMethod.Method.Invoke(instance, new object[] { packet, apiSender as IApiSender })!;
                    await task;
                }
            }
            catch (Exception e)
            {
                apiSender.ErrorReply(routeHeader, (short)BaseErrorCode.UncheckedContentsError);
                LOG.Error(e.StackTrace, this.GetType(), e);
            }
        }

        private void ExtractTypes(Reflections reflections)
        {
            foreach(Type type in reflections.GetTypes())
            {
                _types.Add(type.FullName!, type);
            }
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
            reflections.GetMethodsBySignature("Handles", typeof(void), typeof(IHandlerRegister),typeof(IBackendHandlerRegister)).ForEach(methodInfo =>
            {
                var className = methodInfo.DeclaringType!.FullName!;
                var type = _types[className!]!;
                var handlerRegister = new XHandlerRegister();
                var backendHandlerRegister = new XBackendHandlerRegister();
                var instance = XServiceProvider.Instance.GetRequiredService(type);
                methodInfo.Invoke(instance, new object[]{handlerRegister, backendHandlerRegister });

                
                foreach (var (key,value) in handlerRegister.Handles)
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
}
