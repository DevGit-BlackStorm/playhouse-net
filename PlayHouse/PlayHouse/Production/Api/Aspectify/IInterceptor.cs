namespace PlayHouse.Production.Api.Aspectify;

public interface IInterceptor
{
    Task Intercept(Invocation invocation);
}