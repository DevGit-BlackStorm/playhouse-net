using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Api.Filter;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class ApiActionFilterAttribute : Attribute, IApiFilter
{
    public abstract void BeforeExecution(Packet packet, IApiSender apiSender);
    public abstract void AfterExecution(Packet packet, IApiSender apiSender);
    
    
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class ApiBackendActionFilterAttribute : Attribute, IApiBackendFilter
{
    public abstract void BeforeExecution(Packet packet, IApiBackendSender apiSender);
    public abstract void AfterExecution(Packet packet, IApiBackendSender apiSender);
    

}
