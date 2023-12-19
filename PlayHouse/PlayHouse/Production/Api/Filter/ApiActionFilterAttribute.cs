using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Api.Filter;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class ApiActionFilterAttribute : Attribute, IApiFilter
{
    public abstract void BeforeExecution(IPacket packet, IApiSender apiSender);
    public abstract void AfterExecution(IPacket packet, IApiSender apiSender);
    
    
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class ApiBackendActionFilterAttribute : Attribute, IApiBackendFilter
{
    public abstract void BeforeExecution(IPacket packet, IApiBackendSender apiSender);
    public abstract void AfterExecution(IPacket packet, IApiBackendSender apiSender);
    

}
