using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    internal class ResponseBuilder
    {
        private readonly PNConfiguration config;
        private readonly IJsonPluggableLibrary jsonLib;
        private readonly IPubnubLog pubnubLog;

        public ResponseBuilder(PNConfiguration pubnubConfig, IJsonPluggableLibrary jsonPluggableLibrary, IPubnubLog log)
        {
            this.config = pubnubConfig;
            this.jsonLib = jsonPluggableLibrary;
            this.pubnubLog = log;
        }

        public T JsonToObject<T>(List<object> listObject)
        {
            T ret = default(T);

            if (listObject == null)
            {
                return ret;
            }

            PNPublishResult result = null;
            if (listObject.Count >= 2)
            {
                long publishTimetoken;
                Int64.TryParse(listObject[2].ToString(), out publishTimetoken);
                result = new PNPublishResult
                {
                    Timetoken = publishTimetoken
                };
            }

            ret = (T)Convert.ChangeType(result, typeof(PNPublishResult), CultureInfo.InvariantCulture);

            return ret;
        }
    }
}
