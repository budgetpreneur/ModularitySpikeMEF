using System.Collections;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace PubnubApi
{
    [Export(typeof(IPubnubHttp))]
    public class PubnubHttpClient : IPubnubHttp
    {
        private static HttpClient httpClientSubscribe;
        private static HttpClient httpClientNonsubscribe;
        private static PubnubHttpClientHandler pubnubHttpClientHandler { get; set; }

        public PNConfiguration Config { get; set; }

        public IJsonPluggableLibrary JsonLib { get; set; }
        public ITelemetry TelemetryMgr { get; set; }

        public void PubnubHttpClientInit()
        {
            if (httpClientSubscribe == null && Config != null)
            {
                if (Config.Proxy != null)
                {
                    HttpClientHandler httpClientHandler = new HttpClientHandler();
                    if (httpClientHandler.SupportsProxy)
                    {
                        httpClientHandler.Proxy = Config.Proxy;
                        httpClientHandler.UseProxy = true;
                    }
                    pubnubHttpClientHandler = new PubnubHttpClientHandler("PubnubHttpClientHandler", httpClientHandler, Config, null, null, null);
                    httpClientSubscribe = new HttpClient(pubnubHttpClientHandler);
                }
                else
                {
                    httpClientSubscribe = new HttpClient();
                }
                httpClientSubscribe.DefaultRequestHeaders.Accept.Clear();
                httpClientSubscribe.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClientSubscribe.Timeout = TimeSpan.FromSeconds(Config.SubscribeTimeout);
            }
            if (httpClientNonsubscribe == null && Config != null)
            {
                if (Config.Proxy != null)
                {
                    HttpClientHandler httpClientHandler = new HttpClientHandler();
                    if (httpClientHandler.SupportsProxy)
                    {
                        httpClientHandler.Proxy = Config.Proxy;
                        httpClientHandler.UseProxy = true;
                    }
                    pubnubHttpClientHandler = new PubnubHttpClientHandler("PubnubHttpClientHandler", httpClientHandler, Config, null, null, null);
                    httpClientNonsubscribe = new HttpClient(pubnubHttpClientHandler);
                }
                else
                {
                    httpClientNonsubscribe = new HttpClient();
                }
                httpClientNonsubscribe.DefaultRequestHeaders.Accept.Clear();
                httpClientNonsubscribe.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClientNonsubscribe.Timeout = TimeSpan.FromSeconds(Config.NonSubscribeRequestTimeout);
            }

        }
        public async Task<Tuple<string,int>> SendRequestAndGetJsonResponse<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            return await SendRequestAndGetJsonResponseHttpClient(requestUri, pubnubRequestState, request).ConfigureAwait(false);
        }
    
        public async Task<Tuple<string, int>> SendRequestAndGetJsonResponseWithPATCH<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] patchData)
        {
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, patchData = {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), patchData), Config.LogVerbosity);
            return await SendRequestAndGetJsonResponseHttpClientWithPATCH(requestUri, pubnubRequestState, patchData).ConfigureAwait(false);
        }

        public async Task<Tuple<string, int>> SendRequestAndGetJsonResponseWithPOST<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] postData, string contentType)
        {
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, postData bytearray len= {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), postData.Length), Config.LogVerbosity);
            return await SendRequestAndGetJsonResponseHttpClientWithPOST(requestUri, pubnubRequestState, postData, contentType).ConfigureAwait(false);
        }

        public async Task<byte[]> SendRequestAndGetStreamResponse<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            return await SendRequestAndGetStreamResponseHttpClient(requestUri, pubnubRequestState).ConfigureAwait(false);
        }

        async Task<Tuple<string, int>> SendRequestAndGetJsonResponseHttpClient<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            string jsonString = "";
            int httpStatusCode = 0;
            HttpResponseMessage response = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                PubnubHttpClientInit();
                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, Inside SendRequestAndGetJsonResponseHttpClient", DateTime.Now.ToString(CultureInfo.InvariantCulture)), Config.LogVerbosity);
                cts.CancelAfter(GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000);
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                if (pubnubRequestState.ResponseType == PNOperationType.PNSubscribeOperation)
                {
                    response = await httpClientSubscribe.GetAsync(requestUri, cts.Token).ConfigureAwait(false);
                }
                else if (string.Compare(FindHttpGetOrDeleteMethod(pubnubRequestState), "DELETE", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    response = await httpClientNonsubscribe.DeleteAsync(requestUri, cts.Token).ConfigureAwait(false);
                }
                else
                {
                    response = await httpClientNonsubscribe.GetAsync(requestUri, cts.Token).ConfigureAwait(false);
                }
                httpStatusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode || response.Content != null)
                {
                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    stopWatch.Stop();
                    if (TelemetryMgr != null)
                    {
                        await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType);
                    }
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        pubnubRequestState.GotJsonResponse = true;
                    }
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Got HttpResponseMessage for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), requestUri));
                }
                else
                {
                    stopWatch.Stop();
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, No HttpResponseMessage for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), requestUri));
                }

            }
            catch (HttpRequestException httpReqEx)
            {
                if (httpReqEx.InnerException is WebException)
                {
                    WebException currentWebException = httpReqEx.InnerException as WebException;
                    if (currentWebException != null)
                    {
                        if (currentWebException.Response != null)
                        {
                            pubnubRequestState.Response = currentWebException.Response as HttpWebResponse;
                            using (StreamReader streamReader = new StreamReader(currentWebException.Response.GetResponseStream()))
                            {
                                //Need to return this response 
                                jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                                System.Diagnostics.Debug.WriteLine(jsonString);
                                System.Diagnostics.Debug.WriteLine("");
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON from HttpClient WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                return new Tuple<string,int>(jsonString,httpStatusCode);
                            }
                        }
                    }

                    //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetJsonResponseHttpClient InnerException WebException status {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ((WebException)httpReqEx.InnerException).Status.ToString()), Config.LogVerbosity);
                    throw httpReqEx.InnerException;
                }

                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetJsonResponseHttpClient HttpRequestException {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), httpReqEx.Message), Config.LogVerbosity);
                throw;
            }
            catch (Exception ex)
            {
                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetJsonResponseHttpClient Exception {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ex.Message), Config.LogVerbosity);
                throw;
            }
            finally
            {
                if (response != null && response.Content != null)
                {
                    response.Content.Dispose();
                    pubnubRequestState.Response = null;
                    pubnubRequestState.Request = null;
                }
            }
            return new Tuple<string, int>(jsonString, httpStatusCode);
        }

        async Task<byte[]> SendRequestAndGetStreamResponseHttpClient<T>(Uri requestUri, RequestState<T> pubnubRequestState)
        {
            byte[] streamBytes = null;
            HttpResponseMessage response = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, Inside SendRequestAndGetStreamResponseHttpClient", DateTime.Now.ToString(CultureInfo.InvariantCulture)), Config.LogVerbosity);
                cts.CancelAfter(GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000);
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                response = await httpClientNonsubscribe.GetAsync(requestUri, cts.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode || response.Content != null)
                {
                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    stopWatch.Stop();
                    if (TelemetryMgr != null)
                    {
                        await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType);
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        streamBytes = ms.ToArray();
                    }
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Got HttpResponseMessage for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), requestUri));
                }
                else
                {
                    stopWatch.Stop();
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, No HttpResponseMessage for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), requestUri));
                }

            }
            catch (HttpRequestException httpReqEx)
            {
                if (httpReqEx.InnerException is WebException)
                {
                    WebException currentWebException = httpReqEx.InnerException as WebException;
                    if (currentWebException != null)
                    {
                        if (currentWebException.Response != null)
                        {
                            pubnubRequestState.Response = currentWebException.Response as HttpWebResponse;
                            var errorStream = currentWebException.Response.GetResponseStream();
                            using (MemoryStream ms = new MemoryStream())
                            {
                                errorStream.CopyTo(ms);
                                streamBytes = ms.ToArray();
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved Stream Bytes from HttpClient WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                            }
                        }
                    }

                    //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetStreamResponseHttpClient InnerException WebException status {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ((WebException)httpReqEx.InnerException).Status.ToString()), Config.LogVerbosity);
                    throw httpReqEx.InnerException;
                }

                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetStreamResponseHttpClient HttpRequestException {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), httpReqEx.Message), Config.LogVerbosity);
                throw;
            }
            catch (Exception ex)
            {
                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetStreamResponseHttpClient Exception {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ex.Message), Config.LogVerbosity);
                throw;
            }
            finally
            {
                if (response != null && response.Content != null)
                {
                    response.Content.Dispose();
                    pubnubRequestState.Response = null;
                    pubnubRequestState.Request = null;
                }
            }
            return streamBytes;
        }

        async Task<Tuple<string, int>> SendRequestAndGetJsonResponseHttpClientWithPOST<T>(Uri requestUri, RequestState<T> pubnubRequestState, byte[] postData, string contentType)
        {
            string jsonString = "";
            int httpStatusCode = 0;
            HttpResponseMessage response = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, SendRequestAndGetJsonResponseHttpClientPOST Before httpClient.GetAsync", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                cts.CancelAfter(GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000);
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                ByteArrayContent postDataContent = new ByteArrayContent(postData);
                postDataContent.Headers.Remove("Content-Type");
                if (string.IsNullOrEmpty(contentType))
                {
                    postDataContent.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                }
                else
                {
                    postDataContent.Headers.TryAddWithoutValidation("Content-Type", contentType);
                }
                if (pubnubRequestState.ResponseType == PNOperationType.PNSubscribeOperation)
                {
                    response = await httpClientSubscribe.PostAsync(requestUri, postDataContent, cts.Token).ConfigureAwait(false);
                }
                else
                {
                    response = await httpClientNonsubscribe.PostAsync(requestUri, postDataContent, cts.Token).ConfigureAwait(false);
                }

                if (response.IsSuccessStatusCode || response.Content != null)
                {
                    stopWatch.Stop();
                    if (TelemetryMgr != null)
                    {
                        await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                    }
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Got POST HttpResponseMessage for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), requestUri));
                    if ((int)response.StatusCode == 204 && pubnubRequestState.ResponseType == PNOperationType.PNFileUploadOperation)
                    {
                        return new Tuple<string,int>("{}", (int)response.StatusCode);
                    }
                    else
                    {
                        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        using (StreamReader streamReader = new StreamReader(stream))
                        {
                            jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                            pubnubRequestState.GotJsonResponse = true;
                        }
                    }
                }
                else
                {
                    stopWatch.Stop();
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, No POST HttpResponseMessage for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), requestUri));
                }

            }
            catch (HttpRequestException httpReqEx)
            {
                if (httpReqEx.InnerException is WebException)
                {
                    WebException currentWebException = httpReqEx.InnerException as WebException;
                    if (currentWebException != null)
                    {
                        if (currentWebException.Response != null)
                        {
                            pubnubRequestState.Response = currentWebException.Response as HttpWebResponse;
                            using (StreamReader streamReader = new StreamReader(currentWebException.Response.GetResponseStream()))
                            {
                                //Need to return this response 
                                jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                                System.Diagnostics.Debug.WriteLine(jsonString);
                                System.Diagnostics.Debug.WriteLine("");
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON from HttpClient POST WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                return new Tuple<string, int>(jsonString, httpStatusCode);
                            }
                        }
                    }

                    //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetJsonResponseHttpClientPOST InnerException WebException status {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ((WebException)httpReqEx.InnerException).Status.ToString()), Config.LogVerbosity);
                    throw httpReqEx.InnerException;
                }

                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetJsonResponseHttpClientPOST HttpRequestException {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), httpReqEx.Message), Config.LogVerbosity);
                throw;
            }
            catch (Exception ex)
            {
                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetJsonResponseHttpClientPOST Exception {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ex.Message), Config.LogVerbosity);
                throw;
            }
            finally
            {
                if (response != null && response.Content != null)
                {
                    response.Content.Dispose();
                    pubnubRequestState.Response = null;
                    pubnubRequestState.Request = null;
                }
            }
            return new Tuple<string, int>(jsonString, httpStatusCode);
        }

        async Task<Tuple<string, int>> SendRequestAndGetJsonResponseHttpClientWithPATCH<T>(Uri requestUri, RequestState<T> pubnubRequestState, byte[] patchData)
        {
            string jsonString = "";
            int httpStatusCode = 0;
            HttpResponseMessage response = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, SendRequestAndGetJsonResponseHttpClientWithPATCH Before httpClient.SendAsync", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                cts.CancelAfter(GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000);
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                HttpMethod httpMethod = new HttpMethod("PATCH");
                HttpRequestMessage requestMsg = new HttpRequestMessage(httpMethod, requestUri)
                {
                    Content = new ByteArrayContent(patchData)
                };
                if (pubnubRequestState.ResponseType == PNOperationType.PNSubscribeOperation)
                {
                    response = await httpClientSubscribe.SendAsync(requestMsg, cts.Token).ConfigureAwait(false);
                }
                else
                {
                    response = await httpClientNonsubscribe.SendAsync(requestMsg, cts.Token).ConfigureAwait(false);
                }

                if (response.IsSuccessStatusCode || response.Content != null)
                {
                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    stopWatch.Stop();
                    if (TelemetryMgr != null)
                    {
                        await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                    }
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        pubnubRequestState.GotJsonResponse = true;
                    }
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Got POST HttpResponseMessage for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), requestUri));
                }
                else
                {
                    stopWatch.Stop();
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, No POST HttpResponseMessage for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), requestUri));
                }

            }
            catch (HttpRequestException httpReqEx)
            {
                if (httpReqEx.InnerException is WebException)
                {
                    WebException currentWebException = httpReqEx.InnerException as WebException;
                    if (currentWebException != null)
                    {
                        if (currentWebException.Response != null)
                        {
                            pubnubRequestState.Response = currentWebException.Response as HttpWebResponse;
                            using (StreamReader streamReader = new StreamReader(currentWebException.Response.GetResponseStream()))
                            {
                                //Need to return this response 
                                jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                                System.Diagnostics.Debug.WriteLine(jsonString);
                                System.Diagnostics.Debug.WriteLine("");
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON from HttpClient POST WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                return new Tuple<string, int>(jsonString, httpStatusCode);
                            }
                        }
                    }

                    //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetJsonResponseHttpClientPOST InnerException WebException status {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ((WebException)httpReqEx.InnerException).Status.ToString()), Config.LogVerbosity);
                    throw httpReqEx.InnerException;
                }

                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetJsonResponseHttpClientPOST HttpRequestException {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), httpReqEx.Message), Config.LogVerbosity);
                throw;
            }
            catch (Exception ex)
            {
                //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, SendRequestAndGetJsonResponseHttpClientPOST Exception {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ex.Message), Config.LogVerbosity);
                throw;
            }
            finally
            {
                if (response != null && response.Content != null)
                {
                    response.Content.Dispose();
                    pubnubRequestState.Response = null;
                    pubnubRequestState.Request = null;
                }
            }
            return new Tuple<string, int>(jsonString, httpStatusCode);
        }


        public HttpWebRequest SetNoCache<T>(HttpWebRequest request)
        {
            throw new NotImplementedException();
        }

        public HttpWebRequest SetProxy<T>(HttpWebRequest request)
        {
            throw new NotImplementedException();
        }

        public HttpWebRequest SetServicePointSetTcpKeepAlive(HttpWebRequest request)
        {
            throw new NotImplementedException();
        }

        public HttpWebRequest SetTimeout<T>(RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<Tuple<string, PNStatus>> UrlProcessRequest<T>(Uri requestUri, RequestState<T> pubnubRequestState, bool terminateCurrentSubRequest, byte[] postOrPatchData, string contentType)
        {
            Tuple<string, int> jsonResp;

            if (pubnubRequestState != null && pubnubRequestState.UsePostMethod)
            {
                jsonResp = await SendRequestAndGetJsonResponseWithPOST(requestUri, pubnubRequestState, null, postOrPatchData, contentType).ConfigureAwait(false);
            }
            else if (pubnubRequestState != null && pubnubRequestState.UsePatchMethod)
            {
                jsonResp = await SendRequestAndGetJsonResponseWithPATCH(requestUri, pubnubRequestState, null, postOrPatchData).ConfigureAwait(false);
            }
            else
            {
                jsonResp = await SendRequestAndGetJsonResponse(requestUri, pubnubRequestState, null).ConfigureAwait(false);
            }
            PNStatus status = new PNStatus();
            status.StatusCode = jsonResp.Item2;
            return new Tuple<string, PNStatus>(jsonResp.Item1, status);
        }

            public Task<Tuple<byte[], PNStatus>> UrlProcessRequestForStream<T>(Uri requestUri, RequestState<T> pubnubRequestState, bool terminateCurrentSubRequest, string contentType)
        {
            throw new NotImplementedException();
        }

        protected int GetTimeoutInSecondsForResponseType(PNOperationType type)
        {
            int timeout;
            if (type == PNOperationType.PNSubscribeOperation || type == PNOperationType.Presence)
            {
                timeout = Config.SubscribeTimeout;
            }
            else if (type == PNOperationType.PNGenerateFileUploadUrlOperation)
            {
                timeout = Config.NonSubscribeRequestTimeout * 3;
            }
            else if (type == PNOperationType.PNFileUploadOperation || type == PNOperationType.PNDownloadFileOperation)
            {
                timeout = Config.NonSubscribeRequestTimeout * 25;
            }
            else
            {
                timeout = Config.NonSubscribeRequestTimeout;
            }
            return timeout;
        }

        private static string FindHttpGetOrDeleteMethod<T>(RequestState<T> pubnubRequestState)
        {
            return (pubnubRequestState != null && (pubnubRequestState.ResponseType == PNOperationType.PNDeleteMessageOperation
                                                || pubnubRequestState.ResponseType == PNOperationType.PNDeleteUuidMetadataOperation
                                                || pubnubRequestState.ResponseType == PNOperationType.PNDeleteChannelMetadataOperation
                                                || pubnubRequestState.ResponseType == PNOperationType.PNRemoveMessageActionOperation
                                                || pubnubRequestState.ResponseType == PNOperationType.PNAccessManagerRevokeToken
                                                || pubnubRequestState.ResponseType == PNOperationType.PNDeleteFileOperation)) ? "DELETE" : "GET";

        }

    }
}