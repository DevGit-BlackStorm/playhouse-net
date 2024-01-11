using System.Reflection;

namespace PlayHouse.Production.Api.Aspectify;

public class Invocation
{
    private readonly object _target;
    private readonly MethodInfo _method;
    private readonly object[] _arguments;
    private readonly List<AspectifyAttribute> _interceptors;
    private int _currentInterceptorIndex = -1;

    public dynamic? ReturnValue { get; private set; }

    public Invocation(object target, MethodInfo method, object[] arguments, List<AspectifyAttribute> interceptors)
    {
        _target = target;
        _method = method;
        _arguments = arguments;
        _interceptors = interceptors;
    }

    public MethodInfo Method => _method;
    public object[] Arguments => _arguments;

    public async Task Proceed()
    {
        _currentInterceptorIndex++;
        if (_currentInterceptorIndex < _interceptors.Count)
        {
           await _interceptors[_currentInterceptorIndex].Intercept(this);
        }
        else
        {
            var returnType = _method.ReturnType;

            if (returnType == typeof(Task))
            {
                // 반환 타입이 void
                 await (Task)_method.Invoke(_target, _arguments)!;
            }
            else
            {
                // 반환 타입이 void가 아님
                ReturnValue = await (dynamic) _method.Invoke(_target, _arguments)!;
            }
            
        }
    }
}
