using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    public interface IPubnubUnitTest
    {
        long Timetoken
        {
            get;
            set;
        }

        string RequestId
        {
            get;
            set;
        }

        byte[] IV
        {
            get;
            set;
        }

        bool InternetAvailable
        {
            get;
            set;
        }

        string SdkVersion
        {
            get;
            set;
        }

        bool IncludePnsdk
        {
            get;
            set;
        }

        bool IncludeUuid
        {
            get;
            set;
        }
    }
}
