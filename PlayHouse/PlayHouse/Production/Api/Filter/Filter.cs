using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Api.Filter;

public interface IApiFilter
{
    void BeforeExecution(IPacket packet, IApiSender apiSender);
    void AfterExecution(IPacket packet, IApiSender apiSender);
}

public interface IApiBackendFilter
{
    void BeforeExecution(IPacket packet, IApiBackendSender apiSender);
    void AfterExecution(IPacket packet, IApiBackendSender apiSender);
}
