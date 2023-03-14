using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    public interface ITelemetry
    {
        Task StoreLatency(long latencyMillisec, PNOperationType type);
    }
}
