using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    internal class PubnubHttpClientHandler : DelegatingHandler
    {
        private readonly PNConfiguration pubnubConfig;
        private readonly IPubnubLog pubnubLog;

        private readonly string pubnubHandlerName;

        public PubnubHttpClientHandler(string name, HttpClientHandler innerHandler, PNConfiguration config, IJsonPluggableLibrary jsonPluggableLibrary, IPubnubUnitTest pubnubUnit, IPubnubLog log)
        {
            base.InnerHandler = innerHandler;
            pubnubHandlerName = string.IsNullOrEmpty(name) ? string.Empty : name;
            pubnubConfig = config;
            pubnubLog = log;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime {0} PubnubHttpClientHandler {1} SendAsync ", DateTime.Now.ToString(CultureInfo.InvariantCulture), pubnubHandlerName), pubnubConfig.LogVerbosity);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
