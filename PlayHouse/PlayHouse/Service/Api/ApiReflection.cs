using Playhouse.Protocol;
using PlayHouse.Communicator.Message;
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

    public class ApiInstance
    {
        public object Instance { get; set; }
        public ApiInstance(object instance)
        {
            Instance = instance;
        }
    }

    class Pair<TFirst, TSecond>
    {
        public TFirst First { get; }
        public TSecond Second { get; }

        public Pair(TFirst first, TSecond second)
        {
            First = first;
            Second = second;
        }
    }

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
        public List<Pair<string,Object>> InvokeMethodByName(string methodName, params object[] arguments)
        {
            return _findTypes
                .SelectMany(type => type.GetMethods())
                .Where(m => m.Name == methodName)
                .Select(m =>
                {
                    var instance = Activator.CreateInstance(m.DeclaringType!);
                    var result = m.Invoke(instance, arguments);
                    return new Pair<string, Object>(m.DeclaringType!.FullName!, result!) ;
                })
                .ToList();
        }
    }

    public class ApiReflection
    {
        private readonly Dictionary<string, ApiInstance> _instances = new Dictionary<string, ApiInstance>();
        private readonly List<ApiMethod> _initMethods = new List<ApiMethod>();
        private readonly Dictionary<int, ApiMethod> _methods = new Dictionary<int, ApiMethod>();
        private readonly Dictionary<int, ApiMethod> _backendMethods = new Dictionary<int, ApiMethod>();
        private readonly Dictionary<int, string> _messageIndexChecker = new Dictionary<int, string>();
        private readonly Dictionary<int, string> _messageIndexBackendChecker = new Dictionary<int, string>();

        public ApiReflection()
        {
            var reflections = new Reflections(typeof(IApiService),typeof(IApiBackendService));
            ExtractInstance(reflections);
            ExtractHandlerMethod(reflections);
        }

        public void CallInitMethod(ISystemPanel systemPanel, ISender sender)
        {
            foreach (var targetMethod in _initMethods)
            {
                try
                {
                    if (!_instances.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
                    var apiInstance = _instances[targetMethod.ClassName];
                    targetMethod.Method.Invoke(apiInstance.Instance, new object[] { systemPanel, sender });
                }
                catch (Exception e)
                {
                    LOG.Error(e.StackTrace, this.GetType(), e);
                    Environment.Exit(1);
                }
            }
        }

        public void CallMethod(RouteHeader routeHeader, Packet packet, bool isBackend, AllApiSender apiSender)
        {
            var msgId = routeHeader.GetMsgId();
            var targetMethod = isBackend ? (_backendMethods.ContainsKey(msgId) ? _backendMethods[msgId] : null) : (_methods.ContainsKey(msgId) ? _methods[msgId] : null);
            if (targetMethod == null) throw new ApiException.NotRegisterApiMethod(msgId);

            if (!_instances.ContainsKey(targetMethod.ClassName)) throw new ApiException.NotRegisterApiInstance(targetMethod.ClassName);
            var targetInstance = _instances[targetMethod.ClassName];

            try
            {
                if (isBackend)
                {
                    targetMethod.Method.Invoke(targetInstance.Instance, new object[] { packet, apiSender as IApiBackendSender });
                }
                else
                {
                    targetMethod.Method.Invoke(targetInstance.Instance, new object[] { packet, apiSender as IApiSender });
                }
            }
            catch (Exception e)
            {
                apiSender.ErrorReply(routeHeader, (short)BaseErrorCode.UncheckedContentsError);
                LOG.Error(e.StackTrace, this.GetType(), e);
            }
        }

        private void ExtractInstance(Reflections reflections)
        {

            reflections.InvokeMethodByName("Instance").ForEach(instance=>_instances.Add(instance.First,new ApiInstance(instance.Second)));

            //reflections.InvokeMethodByName<IApiBackendService>("Instance").ForEach(instance => _instances.Add(instance.First, new ApiInstance(instance.Second)));

        }

        private void ExtractHandlerMethod(Reflections reflections)
        {
            RegisterInitMethod(reflections);
            RegisterHandlerMethod(reflections);
            RegisterBackendHandlerMethod(reflections);
        }

        private void RegisterInitMethod(Reflections reflections)
        {
            //var apiServiceReflections = new Reflections(typeof(IApiService));
            reflections.GetMethodsBySignature("Init", typeof(void), typeof(ISystemPanel), typeof(ISender)).ForEach(el => {
                _initMethods.Add(new ApiMethod(0, el.DeclaringType!.FullName!, el));
            });

            //var apiBackendServiceReflections = new Reflections(typeof(IApiBackendService));
            //reflections.GetMethodsBySignature("Init", typeof(void), typeof(ISystemPanel), typeof(ISender)).ForEach(el => {
            //    _initMethods.Add(new ApiMethod(0, el.DeclaringType!.FullName!, el));
            //});
        }

        private void RegisterHandlerMethod(Reflections reflections)
        {
            //var reflections = new Reflections(typeof(IApiService));
            reflections.GetMethodsBySignature("Handles", typeof(void), typeof(IHandlerRegister)).ForEach(methodInfo =>
            {
                var className = methodInfo.DeclaringType!.FullName!;
                var apiInstance = _instances[className!]!;
                var handlerRegister = new XHandlerRegister();
                methodInfo.Invoke(apiInstance.Instance, new object[]{handlerRegister});

                
                foreach (var (key,value) in handlerRegister.Handles)
                {
                    if (_messageIndexChecker.ContainsKey(key))
                    {
                        throw new ApiException($"registered msgId is duplicated - msgId:{key}, methods: {_messageIndexChecker[key]}, {value.GetMethodInfo().Name}");
                    }
                    _methods[key] = new ApiMethod(key, className, value.Method);
                    _messageIndexChecker[key] = value.GetMethodInfo().Name;
                }

            });

        }
        private void RegisterBackendHandlerMethod(Reflections reflections)
        {
            //var reflections = new Reflections(typeof(IApiBackendService));
            reflections.GetMethodsBySignature("Handles", typeof(void), typeof(IBackendHandlerRegister)).ForEach(methodInfo =>
            {
                var className = methodInfo.DeclaringType!.FullName!;
                var apiInstance = _instances[className!]!;
                var handlerRegister = new XBackendHandlerRegister();
                methodInfo.Invoke(apiInstance.Instance, new object[] { handlerRegister });


                foreach (var (key, value) in handlerRegister.Handles)
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


    }
}
