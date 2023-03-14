using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    internal class UrlRequestBuilder
    {
        private PNConfiguration PubnubConfig { get; }
        private IJsonPluggableLibrary JsonLibrary { get; }
        private IPubnubLog PubnubLog { get; }
        private IPubnubUnitTest PubnubUnitTest { get; }
        public UrlRequestBuilder(PNConfiguration config, IJsonPluggableLibrary jsonPluggableLibrary, IPubnubLog log)
        {
            PubnubConfig = config;
            JsonLibrary = jsonPluggableLibrary;
            PubnubLog = log;
        }

        internal Uri BuildPublishRequest(string requestMethod, string requestBody, string channel, object originalMessage, bool storeInHistory, int ttl, Dictionary<string, object> userMetaData, Dictionary<string, string> additionalUrlParams, Dictionary<string, object> externalQueryParam)
        {
            PNOperationType currentType = PNOperationType.PNPublishOperation;

            List<string> url = new List<string>();
            url.Add("publish");
            url.Add(PubnubConfig.PublishKey);
            url.Add(PubnubConfig.SubscribeKey);
            url.Add("0");
            url.Add(channel);
            url.Add("0");
            if (requestMethod.ToUpperInvariant() == "GET")
            {
                string message = JsonEncodePublishMsg(originalMessage);
                url.Add(message);
            }

            Dictionary<string, string> additionalUrlParamsDic = new Dictionary<string, string>();
            if (additionalUrlParams != null)
            {
                additionalUrlParamsDic = additionalUrlParams;
            }

            Dictionary<string, string> requestQueryStringParams = new Dictionary<string, string>(additionalUrlParamsDic);

            //TODO:PANDU
            //if (userMetaData != null)
            //{
            //    string jsonMetaData = JsonLibrary.SerializeToJsonString(userMetaData);
            //    requestQueryStringParams.Add("meta", UriUtil.EncodeUriComponent(jsonMetaData, currentType, false, false, false));
            //}

            if (storeInHistory && ttl >= 0)
            {
                requestQueryStringParams.Add("tt1", ttl.ToString());
            }

            if (!storeInHistory)
            {
                requestQueryStringParams.Add("store", "0");
            }

            if (externalQueryParam != null && externalQueryParam.Count > 0)
            {
                foreach (KeyValuePair<string, object> kvp in externalQueryParam)
                {
                    if (!requestQueryStringParams.ContainsKey(kvp.Key))
                    {
                        //TODO:PANDU
                        //requestQueryStringParams.Add(kvp.Key, UriUtil.EncodeUriComponent(kvp.Value.ToString(), currentType, false, false, false));
                    }
                }
            }

            bool allowPAMv3Sign = requestMethod.ToUpperInvariant() != "POST";
            string queryString = BuildQueryString(currentType, requestQueryStringParams);

            return BuildRestApiRequest(requestMethod, requestBody, url, currentType, queryString, allowPAMv3Sign);
        }

        private Uri BuildRestApiRequest(string requestMethod, string requestBody, List<string> urlComponents, PNOperationType type, string queryString, bool isPamV3Sign)
        {
            StringBuilder url = new StringBuilder();

            if (PubnubConfig.Secure)
            {
                url.Append("https://");
            }
            else
            {
                url.Append("http://");
            }

            url.Append(PubnubConfig.Origin);

            for (int componentIndex = 0; componentIndex < urlComponents.Count; componentIndex++)
            {
                url.Append("/");

                if ((type == PNOperationType.PNPublishOperation || type == PNOperationType.PNPublishFileMessageOperation) && componentIndex == urlComponents.Count - 1)
                {
                    url.Append(UriUtil.EncodeUriComponent(urlComponents[componentIndex], type, false, true, false));
                }
                else if (type == PNOperationType.PNAccessManagerRevokeToken)
                {
                    url.Append(UriUtil.EncodeUriComponent(urlComponents[componentIndex], type, false, false, false));
                }
                else
                {
                    url.Append(UriUtil.EncodeUriComponent(urlComponents[componentIndex], type, true, false, false));
                }
            }

            url.Append("?");
            url.Append(queryString);
            System.Diagnostics.Debug.WriteLine("sb = " + url);
            Uri requestUri = new Uri(url.ToString());

            if (type == PNOperationType.PNPublishOperation || type == PNOperationType.PNPublishFileMessageOperation || type == PNOperationType.PNSubscribeOperation || type == PNOperationType.Presence)
            {
                ForceCanonicalPathAndQuery(requestUri);
            }
            System.Diagnostics.Debug.WriteLine("Uri = " + requestUri.ToString());

            if (PubnubConfig.SecretKey.Length > 0)
            {
                StringBuilder partialUrl = new StringBuilder();
                partialUrl.Append(requestUri.AbsolutePath);

                string signature = "";
                //if (isPamV3Sign)
                //{
                //    signature = GeneratePAMv3Signature(requestMethod, requestBody, queryString, partialUrl.ToString(), type);
                //}
                //else
                //{
                //    signature = GeneratePAMv2Signature(queryString, partialUrl.ToString(), type);
                //}
                string queryStringWithSignature = string.Format("{0}&signature={1}", queryString, signature);
                UriBuilder uriBuilder = new UriBuilder(requestUri);
                uriBuilder.Query = queryStringWithSignature;

                requestUri = uriBuilder.Uri;
            }

            return requestUri;
        }

        private string JsonEncodePublishMsg(object originalMessage)
        {
            string message = JsonLibrary.SerializeToJsonString(originalMessage);

            if (PubnubConfig.CipherKey.Length > 0)
            {
                PubnubCrypto aes = new PubnubCrypto(PubnubConfig.CipherKey, PubnubConfig, PubnubLog, null);
                string encryptMessage = aes.Encrypt(message);
                message = JsonLibrary.SerializeToJsonString(encryptMessage);
            }

            return message;
        }

        private string BuildQueryString(PNOperationType type, Dictionary<string, string> queryStringParamDic)
        {
            string queryString = "";

            try
            {
                Dictionary<string, string> internalQueryStringParamDic = new Dictionary<string, string>();
                if (queryStringParamDic != null)
                {
                    internalQueryStringParamDic = queryStringParamDic;
                }

                string qsUuid = internalQueryStringParamDic.ContainsKey("uuid") ? internalQueryStringParamDic["uuid"] : null;

                Dictionary<string, string> commonQueryStringParams = GenerateCommonQueryParams(type, qsUuid);
                Dictionary<string, string> queryStringParams = new Dictionary<string, string>(commonQueryStringParams.Concat(internalQueryStringParamDic).GroupBy(item => item.Key).ToDictionary(item => item.Key, item => item.First().Value));

                queryString = string.Join("&", queryStringParams.OrderBy(kvp => kvp.Key, StringComparer.Ordinal).Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)).ToArray());

            }
            catch (Exception ex)
            {
                LoggingMethod.WriteToLog(PubnubLog, "UrlRequestBuilder => BuildQueryString error " + ex, (PubnubConfig != null) ? PubnubConfig.LogVerbosity : PNLogVerbosity.BODY);
            }

            return queryString;
        }

        private Dictionary<string, string> GenerateCommonQueryParams(PNOperationType type, string uuid)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            long timeStamp = TranslateUtcDateTimeToSeconds(DateTime.UtcNow);
            string requestid = Guid.NewGuid().ToString();

            //TODO:PANDU
            //if (PubnubUnitTest != null)
            //{
            //    timeStamp = PubnubUnitTest.Timetoken;
            //    requestid = string.IsNullOrEmpty(PubnubUnitTest.RequestId) ? "" : PubnubUnitTest.RequestId;
            //}

            //if (PubnubUnitTest != null && pubnubConfig.ContainsKey(pubnubInstanceId))
            //{
            //    if (PubnubUnitTest.IncludeUuid)
            //    {
            //        ret.Add("uuid", UriUtil.EncodeUriComponent(pubnubConfig[pubnubInstanceId].Uuid, PNOperationType.PNSubscribeOperation, false, false, true));
            //    }

            //    if (pubnubUnitTest.IncludePnsdk)
            //    {
            //        ret.Add("pnsdk", UriUtil.EncodeUriComponent(Pubnub.Version, PNOperationType.PNSubscribeOperation, false, false, true));
            //    }
            //}
            //else
            //{
            //    ret.Add("uuid", UriUtil.EncodeUriComponent(uuid != null ? uuid :
            //                            (pubnubConfig.ContainsKey(pubnubInstanceId) ? pubnubConfig[pubnubInstanceId].Uuid : ""),
            //                            PNOperationType.PNSubscribeOperation, false, false, true));
            //    ret.Add("pnsdk", UriUtil.EncodeUriComponent(Pubnub.Version, PNOperationType.PNSubscribeOperation, false, false, true));
            //}

            //if (PubnubConfig != null)
            //{
            //    if (pubnubConfig.ContainsKey(pubnubInstanceId) && pubnubConfig[pubnubInstanceId].IncludeRequestIdentifier)
            //    {
            //        ret.Add("requestid", requestid);
            //    }

            //    if (pubnubConfig.ContainsKey(pubnubInstanceId) && pubnubConfig[pubnubInstanceId].IncludeInstanceIdentifier && !string.IsNullOrEmpty(pubnubInstanceId) && pubnubInstanceId.Trim().Length > 0)
            //    {
            //        ret.Add("instanceid", pubnubInstanceId);
            //    }

            //    if (pubnubConfig.ContainsKey(pubnubInstanceId) && pubnubConfig[pubnubInstanceId].EnableTelemetry && telemetryMgr != null)
            //    {
            //        Dictionary<string, string> opsLatency = telemetryMgr.GetOperationsLatency().ConfigureAwait(false).GetAwaiter().GetResult();
            //        if (opsLatency != null && opsLatency.Count > 0)
            //        {
            //            foreach (string key in opsLatency.Keys)
            //            {
            //                ret.Add(key, opsLatency[key]);
            //            }
            //        }
            //    }

            //    if (pubnubConfig.ContainsKey(pubnubInstanceId) && !string.IsNullOrEmpty(pubnubConfig[pubnubInstanceId].SecretKey))
            //    {
            //        ret.Add("timestamp", timeStamp.ToString());
            //    }

            //    if (type != PNOperationType.PNTimeOperation
            //            && type != PNOperationType.PNAccessManagerGrant && type != PNOperationType.PNAccessManagerGrantToken && type != PNOperationType.PNAccessManagerRevokeToken && type != PNOperationType.ChannelGroupGrantAccess
            //            && type != PNOperationType.PNAccessManagerAudit && type != PNOperationType.ChannelGroupAuditAccess)
            //    {
            //        if (tokenMgr != null && !string.IsNullOrEmpty(tokenMgr.AuthToken) && tokenMgr.AuthToken.Trim().Length > 0)
            //        {
            //            ret.Add("auth", UriUtil.EncodeUriComponent(tokenMgr.AuthToken, type, false, false, false));
            //        }
            //        else if (pubnubConfig.ContainsKey(pubnubInstanceId) && !string.IsNullOrEmpty(pubnubConfig[pubnubInstanceId].AuthKey) && pubnubConfig[pubnubInstanceId].AuthKey.Trim().Length > 0)
            //        {
            //            ret.Add("auth", UriUtil.EncodeUriComponent(pubnubConfig[pubnubInstanceId].AuthKey, type, false, false, false));
            //        }
            //    }
            //}

            return ret;
        }

        private void ForceCanonicalPathAndQuery(Uri requestUri)
        {
#if !NETSTANDARD10 && !NETSTANDARD11 && !NETSTANDARD12 && !WP81
            //LoggingMethod.WriteToLog(pubnubLog, "Inside ForceCanonicalPathAndQuery = " + requestUri.ToString(), pubnubConfig.ContainsKey(pubnubInstanceId) ? pubnubConfig[pubnubInstanceId].LogVerbosity : PNLogVerbosity.NONE);
            try
            {
                FieldInfo flagsFieldInfo = typeof(Uri).GetField("m_Flags", BindingFlags.Instance | BindingFlags.NonPublic);
                if (flagsFieldInfo != null)
                {
                    ulong flags = (ulong)flagsFieldInfo.GetValue(requestUri);
                    flags &= ~((ulong)0x30); // Flags.PathNotCanonical|Flags.QueryNotCanonical
                    flagsFieldInfo.SetValue(requestUri, flags);
                }
            }
            catch (Exception ex)
            {
                //LoggingMethod.WriteToLog(pubnubLog, "Exception Inside ForceCanonicalPathAndQuery = " + ex, pubnubConfig.ContainsKey(pubnubInstanceId) ? pubnubConfig[pubnubInstanceId].LogVerbosity : PNLogVerbosity.BODY);
            }
#endif
        }
        public static long TranslateUtcDateTimeToSeconds(DateTime dotNetUTCDateTime)
        {
            TimeSpan timeSpan = dotNetUTCDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long timeStamp = Convert.ToInt64(timeSpan.TotalSeconds);
            return timeStamp;
        }
    }
}
