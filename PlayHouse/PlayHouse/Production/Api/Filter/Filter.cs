using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Api.Filter;

public interface IApiFilter
{
    void BeforeExecution(Packet packet, IApiSender apiSender);
    void AfterExecution(Packet packet, IApiSender apiSender);
}

public interface IApiBackendFilter
{
    void BeforeExecution(Packet packet, IApiBackendSender apiSender);
    void AfterExecution(Packet packet, IApiBackendSender apiSender);
}
