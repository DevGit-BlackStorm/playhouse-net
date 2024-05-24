using System.Reflection;

namespace PlayHouse.Production.Api.Aspectify;

public class Invocation
{
    private readonly List<AspectifyAttribute> _interceptors;
    private readonly object _target;
    private int _currentInterceptorIndex = -1;

    public Invocation(
        object target,
        MethodInfo method,
        object[] arguments,
        List<AspectifyAttribute> interceptors,
        IServiceProvider serviceProvider)
    {
        _target = target;
        Method = method;
        Arguments = arguments;
        _interceptors = interceptors;
        ServiceProvider = serviceProvider;
    }

    public dynamic? ReturnValue { get; private set; }

    public MethodInfo Method { get; }

    public object[] Arguments { get; }

    public IServiceProvider ServiceProvider { get; }

    public async Task Proceed()
    {
        _currentInterceptorIndex++;
        if (_currentInterceptorIndex < _interceptors.Count)
        {
            await _interceptors[_currentInterceptorIndex].Intercept(this);
        }
        else
        {
            var returnType = Method.ReturnType;

            if (returnType == typeof(Task))
            {
                // 반환 타입이 void
                await (Task)Method.Invoke(_target, Arguments)!;
            }
            else
            {
                // 반환 타입이 void가 아님
                ReturnValue = await (dynamic)Method.Invoke(_target, Arguments)!;
            }
        }
    }
}