using PlayHouse.Production;
using PlayHouse.Production.Api.Aspectify;
using PlayHouse.Production.Api.Filter;


namespace PlayHouseTests.Service.Api.plain;

[AttributeUsage(AttributeTargets.Class)]
public class TestActionFilterAttribute : AspectifyAttribute
{

    public override async Task Intercept(Invocation invocation)
    {
        IPacket packet = (IPacket)invocation.Arguments[0];
        ReflectionTestResult.ResultMap[$"TestApiActionAttributeBefore_{packet.MsgId}"] = "BeforeExecution";

        await invocation.Proceed();

        ReflectionTestResult.ResultMap[$"TestApiActionAttributeAfter_{packet.MsgId}"] =  "AfterExecution";
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class TestBackendActionFilterAttribute : AspectifyAttribute
{

    public override async Task Intercept(Invocation invocation)
    {
        IPacket packet = (IPacket)invocation.Arguments[0];
        ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeBefore_{packet.MsgId}"] =  "BeforeExecution";
        await invocation.Proceed();
        ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeAfter_{packet.MsgId}"] = "AfterExecution";
    }
}


[AttributeUsage(AttributeTargets.Method)]
public class TestMethodActionFilterAttribute : AspectifyAttribute
{
    public override async Task Intercept(Invocation invocation)
    {
        IPacket packet = (IPacket)invocation.Arguments[0];
        ReflectionTestResult.ResultMap[$"TestApiMethodActionAttributeBefore_{packet.MsgId}"] = "BeforeExecution";
        await invocation.Proceed();
        ReflectionTestResult.ResultMap[$"TestApiMethodActionAttributeAfter_{packet.MsgId}"] = "AfterExecution";
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class TestBackendMethodActionFilterAttribute : AspectifyAttribute
{
    public override async Task Intercept(Invocation invocation)
    {
        IPacket packet = (IPacket)invocation.Arguments[0];
        ReflectionTestResult.ResultMap[$"TestBackendApiMethodActionAttributeBefore_{packet.MsgId}"] = "BeforeExecution";
        await invocation.Proceed();
        ReflectionTestResult.ResultMap[$"TestBackendApiMethodActionAttributeAfter_{packet.MsgId}"] = "AfterExecution";
    }
}

