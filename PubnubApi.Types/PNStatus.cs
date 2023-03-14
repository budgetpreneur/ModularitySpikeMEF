using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    public class PNStatus
    {
        private object savedEndpointOperation { get; set; }

        public PNStatus() { }

        internal PNStatus(object endpointOperation)
        {
            this.savedEndpointOperation = endpointOperation;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public PNStatusCategory Category { get; internal set; }

        public PNErrorData ErrorData { get; set; }
        public bool Error { get;  set; }

        public int StatusCode { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PNOperationType Operation { get; internal set; }

        public bool TlsEnabled { get; internal set; }

        public string Uuid { get; internal set; }
        public string AuthKey { get; internal set; }
        public string Origin { get; internal set; }
        public object ClientRequest { get; internal set; }

        // send back channel, channel groups that were affected by this operation
        public List<string> AffectedChannels { get; internal set; } = new List<string>();
        public List<string> AffectedChannelGroups { get; internal set; } = new List<string>();

        public object AdditonalData { get; internal set; } = new object();

        public void Retry()
        {
            throw new NotImplementedException("Retry in PNStatus Not Implemented");
        }

    }
}
