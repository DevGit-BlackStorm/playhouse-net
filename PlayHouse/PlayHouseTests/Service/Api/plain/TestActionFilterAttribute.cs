using PlayHouse.Production;
using PlayHouse.Production.Api.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouseTests.Service.Api.plain;

[AttributeUsage(AttributeTargets.Class)]
public class TestActionFilterAttribute : ApiActionFilterAttribute
{

    public override void BeforeExecution(IPacket packet, IApiSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestApiActionAttributeBefore_{packet.MsgId}", "BeforeExecution");
    }
    public override void AfterExecution(IPacket packet, IApiSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestApiActionAttributeAfter_{packet.MsgId}", "AfterExecution");
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class TestBackendActionFilterAttribute : ApiBackendActionFilterAttribute
{

    public override void BeforeExecution(IPacket packet, IApiBackendSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestBackendApiActionAttributeBefore_{packet.MsgId}", "BeforeExecution");
    }
    public override void AfterExecution(IPacket packet, IApiBackendSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestBackendApiActionAttributeAfter_{packet.MsgId}", "AfterExecution");
    }
}


[AttributeUsage(AttributeTargets.Method)]
public class TestMethodActionFilterAttribute : ApiActionFilterAttribute
{

    public override void BeforeExecution(IPacket packet, IApiSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestApiMethodActionAttributeBefore_{packet.MsgId}", "BeforeExecution");
    }
    public override void AfterExecution(IPacket packet, IApiSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestApiMethodActionAttributeAfter_{packet.MsgId}", "AfterExecution");
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class TestBackendMethodActionFilterAttribute : ApiBackendActionFilterAttribute
{

    public override void BeforeExecution(IPacket packet, IApiBackendSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestBackendApiMethodActionAttributeBefore_{packet.MsgId}", "BeforeExecution");
    }
    public override void AfterExecution(IPacket packet, IApiBackendSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestBackendApiMethodActionAttributeAfter_{packet.MsgId}", "AfterExecution");
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class TestApiGlobalActionAttribute : ApiActionFilterAttribute
{

    public override void BeforeExecution(IPacket packet, IApiSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestApiGlobalActionAttributeBefore_{packet.MsgId}", "BeforeExecution");
    }
    public override void AfterExecution(IPacket packet, IApiSender apiSender)
    {
        ReflectionTestResult.ResultMap.Add($"TestApiGlobalActionAttributeAfter_{packet.MsgId}", "AfterExecution");
    }
}
