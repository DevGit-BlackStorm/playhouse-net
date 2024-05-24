namespace PlayHouse.Production.Api.Aspectify;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class AspectifyAttribute : Attribute, IInterceptor
{
    public abstract Task Intercept(Invocation invocation);
}