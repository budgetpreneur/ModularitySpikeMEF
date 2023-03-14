using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    public interface IPNConfiguration
    {
        string Origin { get; set; }
        int PresenceTimeout { get; set; }
        int PresenceInterval { get; }
        bool Secure { get; set; }
        string SubscribeKey { get; set; }
        string PublishKey { get; set; }
        string SecretKey { get; set; }
        string CipherKey { get; set; }
        string AuthKey { get; set; }
        string Uuid { get; set; }

        PNLogVerbosity LogVerbosity { get; set; }
    }
}
