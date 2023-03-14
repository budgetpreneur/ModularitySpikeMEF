using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    public interface IPublishOperation
    {
        PNConfiguration Config { get; set; }
        IPublishOperation Message(object message);
        IPublishOperation Channel(string channelName);
        IPublishOperation ShouldStore(bool store);
        IPublishOperation Meta(Dictionary<string, object> metadata);
        IPublishOperation UsePOST(bool post);
        IPublishOperation Ttl(int ttl);
        IPublishOperation QueryParam(Dictionary<string, object> customQueryParam);
        Task<PNResult<PNPublishResult>> ExecuteAsync();
    }
}
