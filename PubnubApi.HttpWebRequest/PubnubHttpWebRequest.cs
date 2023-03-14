using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PubnubApi
{
    [Export(typeof(IPubnubHttp))]
    public class PubnubHttpWebRequest : IPNDataService, IPubnubHttp
    {
        public PNConfiguration Config { get; set; }

        public ITelemetry TelemetryMgr { get; set; }

        public PubnubHttpWebRequest()
        {

        }

        public async Task<string> SendRequestAndGetJsonResponse<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            if (Config.UseClassicHttpWebRequest)
            {
                return await SendRequestAndGetJsonResponseClassicHttp(requestUri, pubnubRequestState, request).ConfigureAwait(false);
            }
            else
            {
                return await SendRequestAndGetJsonResponseTaskFactory(pubnubRequestState, request).ConfigureAwait(false);
            }
        }

        public async Task<string> SendRequestAndGetJsonResponseWithPATCH<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] patchData)
        {
            if (Config.UseClassicHttpWebRequest)
            {
                return await SendRequestAndGetJsonResponseClassicHttpWithPATCH(pubnubRequestState, request, patchData).ConfigureAwait(false);
            }
            else
            {
                return await SendRequestAndGetJsonResponseTaskFactoryWithPATCH(pubnubRequestState, request, patchData).ConfigureAwait(false);
            }
        }

        public async Task<string> SendRequestAndGetJsonResponseWithPOST<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] postData, string contentType)
        {
            if (Config.UseClassicHttpWebRequest)
            {
                return await SendRequestAndGetJsonResponseClassicHttpWithPOST(pubnubRequestState, request, postData, contentType).ConfigureAwait(false);
            }
            else
            {
                return await SendRequestAndGetJsonResponseTaskFactoryWithPOST(pubnubRequestState, request, postData, contentType).ConfigureAwait(false);
            }
        }

        public async Task<byte[]> SendRequestAndGetStreamResponse<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            if (Config.UseClassicHttpWebRequest)
            {
                return await SendRequestAndGetStreamResponseClassicHttp(pubnubRequestState, request).ConfigureAwait(false);
            }
            else
            {
                return await SendRequestAndGetStreamResponseTaskFactory(pubnubRequestState, request).ConfigureAwait(false);
            }
        }

        async Task<string> SendRequestAndGetJsonResponseTaskFactory<T>(RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            HttpWebResponse response = null;
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, Inside SendRequestAndGetJsonResponseTaskFactory", DateTime.Now.ToString(CultureInfo.InvariantCulture)), pubnubConfig.LogVerbosity);
            try
            {
                request.Method = FindHttpGetOrDeleteMethod(pubnubRequestState);
                new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                response = await Task.Factory.FromAsync<HttpWebResponse>(request.BeginGetResponse, asyncPubnubResult => (HttpWebResponse)request.EndGetResponse(asyncPubnubResult), pubnubRequestState).ConfigureAwait(false);
                stopWatch.Stop();
                if (Config.EnableTelemetry && TelemetryMgr != null)
                {
                    await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                }
                //pubnubRequestState.Response = response;
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Got PubnubWebResponse for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), request.RequestUri.ToString()));
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    //Need to return this response 
#if NET35 || NET40
                    string jsonString = streamReader.ReadToEnd();
#else
                    string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                    System.Diagnostics.Debug.WriteLine(jsonString);
                    //pubnubRequestState.GotJsonResponse = true;
                    System.Diagnostics.Debug.WriteLine("");
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON", DateTime.Now.ToString(CultureInfo.InvariantCulture)));

                    if (pubnubRequestState.Response != null)
                    {
#if NET35 || NET40 || NET45 || NET461
                        pubnubRequestState.Response.Close();
#endif
                        //pubnubRequestState.Response = null;
                        //pubnubRequestState.Request = null;
                    }

                    return jsonString;
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    //pubnubRequestState.Response = ex.Response as HttpWebResponse;
                    using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        //Need to return this response 
#if NET35 || NET40
                        string jsonString = streamReader.ReadToEnd();
#else
                        string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON from WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                        return jsonString;
                    }
                }

                if (ex.Message.IndexOf("The request was aborted: The request was canceled") == -1
                                && ex.Message.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                {
                    throw;
                }
                return "";
            }
            catch
            {
                throw;
            }
        }

        async Task<byte[]> SendRequestAndGetStreamResponseTaskFactory<T>(RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            HttpWebResponse response = null;
            byte[] streamBytes;
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, Inside SendRequestAndGetStreamResponseTaskFactory", DateTime.Now.ToString(CultureInfo.InvariantCulture)), pubnubConfig.LogVerbosity);
            try
            {
                request.Method = FindHttpGetOrDeleteMethod(pubnubRequestState);
                new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                response = await Task.Factory.FromAsync<HttpWebResponse>(request.BeginGetResponse, asyncPubnubResult => (HttpWebResponse)request.EndGetResponse(asyncPubnubResult), pubnubRequestState).ConfigureAwait(false);
                stopWatch.Stop();
                if (Config.EnableTelemetry && TelemetryMgr != null)
                {
                    await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                }
                //pubnubRequestState.Response = response;
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Got PubnubWebResponse for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), request.RequestUri.ToString()));
                int statusCode = (int)pubnubRequestState.Response.StatusCode;
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, status code = {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), statusCode));
                using (Stream stream = response.GetResponseStream())
                {
                    long totalSize = 0;
                    long receivedSize = 0;
                    //Allocate 1K buffer
                    byte[] buffer = new byte[1024];
                    using (MemoryStream ms = new MemoryStream())
                    {
#if NET35 || NET40
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
#else
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
#endif
                        receivedSize += bytesRead;
                        while (bytesRead > 0)
                        {
                            ms.Write(buffer, 0, bytesRead);
                            bytesRead = stream.Read(buffer, 0, buffer.Length);
                            receivedSize += bytesRead;
                        }
                        streamBytes = ms.ToArray();
                    }
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, totalsize = {1}; received = {2}", DateTime.Now.ToString(CultureInfo.InvariantCulture), totalSize, receivedSize));
                    //Need to return this response 
                    //pubnubRequestState.GotJsonResponse = true;
                    System.Diagnostics.Debug.WriteLine("");
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved Stream", DateTime.Now.ToString(CultureInfo.InvariantCulture)));

                    if (pubnubRequestState.Response != null)
                    {
#if NET35 || NET40 || NET45 || NET461
                        pubnubRequestState.Response.Close();
#endif
                        //pubnubRequestState.Response = null;
                        //pubnubRequestState.Request = null;
                    }

                    return streamBytes;
                }
            }
            catch (WebException ex)
            {
                if (ex.Message.IndexOf("The request was aborted: The request was canceled") == -1
                                && ex.Message.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                {
                    throw;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Exception in SendRequestAndGetStreamResponseTaskFactory {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ex));
                throw;
            }
        }

        async Task<string> SendRequestAndGetJsonResponseTaskFactoryWithPOST<T>(RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] postData, string contentType)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Before Task.Factory.FromAsync With POST", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
            try
            {
                request.Method = "POST";
                Timer webRequestTimer = new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);

                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                request.ContentType = contentType;

                using (var requestStream = await Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, pubnubRequestState).ConfigureAwait(false))
                {
#if NET35 || NET40
                    requestStream.Write(postData, 0, postData.Length);
                    requestStream.Flush();
#else
                    await requestStream.WriteAsync(postData, 0, postData.Length).ConfigureAwait(false);
                    await requestStream.FlushAsync().ConfigureAwait(false);
#endif

                }

                WebResponse response = await Task.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, pubnubRequestState).ConfigureAwait(false);
                stopWatch.Stop();
                if (TelemetryMgr != null)
                {
                    await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                }
                //pubnubRequestState.Response = response as HttpWebResponse;
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Got PubnubWebResponse With POST for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), request.RequestUri.ToString()));
                int statusCode = (int)pubnubRequestState.Response.StatusCode;
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, statusCode {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), statusCode));
                if (statusCode == 204 && pubnubRequestState.ResponseType == PNOperationType.PNFileUploadOperation)
                {
                    return "{}";
                }
                else
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        //Need to return this response 
#if NET35 || NET40
                        string jsonString = streamReader.ReadToEnd();
#else
                        string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON With POST", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                        //pubnubRequestState.GotJsonResponse = true;

                        if (pubnubRequestState.Response != null)
                        {
#if NET35 || NET40 || NET45 || NET461
                            pubnubRequestState.Response.Close();
#endif
                            //pubnubRequestState.Response = null;
                            //pubnubRequestState.Request = null;
                        }

                        return jsonString;
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    //pubnubRequestState.Response = ex.Response as HttpWebResponse;
                    using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        //Need to return this response 
#if NET35 || NET40
                        string jsonString = streamReader.ReadToEnd();
#else
                        string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON  With POST from WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                        return jsonString;
                    }
                }

                if (ex.Message.IndexOf("The request was aborted: The request was canceled") == -1
                                && ex.Message.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                {
                    throw;
                }
                return "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Exception in SendRequestAndGetJsonResponseTaskFactoryWithPOST {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ex));
                throw;
            }
        }

        async Task<string> SendRequestAndGetJsonResponseTaskFactoryWithPATCH<T>(RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] patchData)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Before Task.Factory.FromAsync With PATCH", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
            try
            {
                request.Method = "PATCH";
                Timer webRequestTimer = new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);

                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                request.ContentType = "application/json";

                using (var requestStream = await Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, pubnubRequestState).ConfigureAwait(false))
                {
#if NET35 || NET40
                    requestStream.Write(patchData, 0, patchData.Length);
                    requestStream.Flush();
#else
                    await requestStream.WriteAsync(patchData, 0, patchData.Length).ConfigureAwait(false);
                    await requestStream.FlushAsync().ConfigureAwait(false);
#endif

                }

                WebResponse response = await Task.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, pubnubRequestState).ConfigureAwait(false);
                stopWatch.Stop();
                if (TelemetryMgr != null)
                {
                    await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                }
                //pubnubRequestState.Response = response as HttpWebResponse;
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Got PubnubWebResponse With PATCH for {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), request.RequestUri.ToString()));
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    //Need to return this response 
#if NET35 || NET40
                    string jsonString = streamReader.ReadToEnd();
#else
                    string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                    System.Diagnostics.Debug.WriteLine(jsonString);
                    System.Diagnostics.Debug.WriteLine("");
                    System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON With PATCH", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                    //pubnubRequestState.GotJsonResponse = true;

                    if (pubnubRequestState.Response != null)
                    {
#if NET35 || NET40 || NET45 || NET461
                        pubnubRequestState.Response.Close();
#endif
                        //pubnubRequestState.Response = null;
                        //pubnubRequestState.Request = null;
                    }

                    return jsonString;
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    //pubnubRequestState.Response = ex.Response as HttpWebResponse;
                    using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        //Need to return this response 
#if NET35 || NET40
                        string jsonString = streamReader.ReadToEnd();
#else
                        string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON  With PATCH from WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                        return jsonString;
                    }
                }

                if (ex.Message.IndexOf("The request was aborted: The request was canceled") == -1
                                && ex.Message.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                {
                    throw;
                }
                return "";
            }
            catch
            {
                throw;
            }
        }

        async Task<string> SendRequestAndGetJsonResponseClassicHttp<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, Inside SendRequestAndGetJsonResponseClassicHttp", DateTime.Now.ToString(CultureInfo.InvariantCulture)), pubnubConfig.LogVerbosity);
            var taskComplete = new TaskCompletionSource<string>();
            try
            {
                request.Method = FindHttpGetOrDeleteMethod<T>(pubnubRequestState);
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Before BeginGetResponse", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                request.BeginGetResponse(new AsyncCallback(
                    async (asynchronousResult) => {
                        RequestState<T> asyncRequestState = asynchronousResult.AsyncState as RequestState<T>;
                        HttpWebRequest asyncWebRequest = asyncRequestState.Request as HttpWebRequest;
                        if (asyncWebRequest != null)
                        {
                            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Before EndGetResponse", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                            HttpWebResponse asyncWebResponse = (HttpWebResponse)asyncWebRequest.EndGetResponse(asynchronousResult);
                            stopWatch.Stop();
                            if (TelemetryMgr != null)
                            {
                                await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                            }
                            //asyncRequestState.Response = asyncWebResponse;
                            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, After EndGetResponse", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                            using (StreamReader streamReader = new StreamReader(asyncWebResponse.GetResponseStream()))
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Inside StreamReader", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                //Need to return this response 
                                string jsonString = streamReader.ReadToEnd();
                                //asyncRequestState.GotJsonResponse = true;

                                System.Diagnostics.Debug.WriteLine(jsonString);
                                System.Diagnostics.Debug.WriteLine("");
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                taskComplete.TrySetResult(jsonString);
                            }
                            if (asyncRequestState.Response != null)
                            {
#if NET35 || NET40 || NET45 || NET461
                                pubnubRequestState.Response.Close();
#endif
                                //asyncRequestState.Response = null;
                                //asyncRequestState.Request = null;
                            }
                        }
                    }
                    ), pubnubRequestState);

                Timer webRequestTimer = new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);
                return taskComplete.Task.Result;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    //pubnubRequestState.Response = ex.Response as HttpWebResponse;
                    using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        //Need to return this response 
#if NET35 || NET40
                        await Task.Factory.StartNew(() => { }).ConfigureAwait(false);
                        string jsonString = streamReader.ReadToEnd();
#else
                        string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON from WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                        return jsonString;
                    }
                }

                if (ex.Message.IndexOf("The request was aborted: The request was canceled") == -1
                                && ex.Message.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                {
                    taskComplete.TrySetException(ex);
                }
                return "";
            }
            catch (Exception ex)
            {
                taskComplete.TrySetException(ex);
                return "";
            }
        }

        async Task<byte[]> SendRequestAndGetStreamResponseClassicHttp<T>(RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, Inside SendRequestAndGetStreamResponseClassicHttp", DateTime.Now.ToString(CultureInfo.InvariantCulture)), pubnubConfig.LogVerbosity);
            var taskComplete = new TaskCompletionSource<byte[]>();
            try
            {
                request.Method = FindHttpGetOrDeleteMethod<T>(pubnubRequestState);
                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Before BeginGetResponse", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                request.BeginGetResponse(new AsyncCallback(
                    async (asynchronousResult) => {
                        RequestState<T> asyncRequestState = asynchronousResult.AsyncState as RequestState<T>;
                        HttpWebRequest asyncWebRequest = asyncRequestState.Request as HttpWebRequest;
                        if (asyncWebRequest != null)
                        {
                            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Before EndGetResponse", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                            HttpWebResponse asyncWebResponse = (HttpWebResponse)asyncWebRequest.EndGetResponse(asynchronousResult);
                            stopWatch.Stop();
                            if (TelemetryMgr != null)
                            {
                                await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                            }
                            //asyncRequestState.Response = asyncWebResponse;
                            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, After EndGetResponse", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                            using (StreamReader streamReader = new StreamReader(asyncWebResponse.GetResponseStream()))
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Inside StreamReader", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                //Need to return this response 
                                string jsonString = streamReader.ReadToEnd();
                                //asyncRequestState.GotJsonResponse = true;

                                System.Diagnostics.Debug.WriteLine(jsonString);
                                System.Diagnostics.Debug.WriteLine("");
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                taskComplete.TrySetResult(null);
                            }
                            if (asyncRequestState.Response != null)
                            {
#if NET35 || NET40 || NET45 || NET461
                                pubnubRequestState.Response.Close();
#endif
                                //asyncRequestState.Response = null;
                                //asyncRequestState.Request = null;
                            }
                        }
                    }
                    ), pubnubRequestState);

                Timer webRequestTimer = new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);
                return taskComplete.Task.Result;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    //pubnubRequestState.Response = ex.Response as HttpWebResponse;
                    using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        //Need to return this response 
#if NET35 || NET40
                        await Task.Factory.StartNew(() => { }).ConfigureAwait(false);
                        string jsonString = streamReader.ReadToEnd();
#else
                        string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON from WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                        return null;
                    }
                }

                if (ex.Message.IndexOf("The request was aborted: The request was canceled") == -1
                                && ex.Message.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                {
                    taskComplete.TrySetException(ex);
                }
                return null;
            }
            catch (Exception ex)
            {
                taskComplete.TrySetException(ex);
                return null;
            }
        }

        async Task<string> SendRequestAndGetJsonResponseClassicHttpWithPOST<T>(RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] postData, string contentType)
        {
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, Inside SendRequestAndGetJsonResponseClassicHttpWithPOST", DateTime.Now.ToString(CultureInfo.InvariantCulture)), pubnubConfig.LogVerbosity);
            var taskComplete = new TaskCompletionSource<string>();
            try
            {
                request.Method = "POST";
                request.ContentType = contentType;

                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
#if !NET35 && !NET40 && !NET45 && !NET461
                using (var requestStream = await Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, pubnubRequestState).ConfigureAwait(false))
                {
                    requestStream.Write(postData, 0, postData.Length);
                    requestStream.Flush();
                }
#else
                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postData, 0, postData.Length);
                    requestStream.Flush();
                }
#endif

                IAsyncResult asyncResult = request.BeginGetResponse(new AsyncCallback(
                    async (asynchronousResult) => {
                        RequestState<T> asyncRequestState = asynchronousResult.AsyncState as RequestState<T>;
                        HttpWebRequest asyncWebRequest = asyncRequestState.Request as HttpWebRequest;
                        if (asyncWebRequest != null)
                        {
                            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Before EndGetResponse With POST ", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                            HttpWebResponse asyncWebResponse = (HttpWebResponse)asyncWebRequest.EndGetResponse(asynchronousResult);
                            stopWatch.Stop();
                            if (TelemetryMgr != null)
                            {
                                await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                            }
                            //asyncRequestState.Response = asyncWebResponse;
                            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, After EndGetResponse With POST ", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                            using (StreamReader streamReader = new StreamReader(asyncWebResponse.GetResponseStream()))
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Inside StreamReader With POST ", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                //Need to return this response 
                                string jsonString = streamReader.ReadToEnd();
                                //asyncRequestState.GotJsonResponse = true;

                                System.Diagnostics.Debug.WriteLine(jsonString);
                                System.Diagnostics.Debug.WriteLine("");
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON With POST ", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                taskComplete.TrySetResult(jsonString);
                            }
                            if (asyncRequestState.Response != null)
                            {
#if NET35 || NET40 || NET45 || NET461
                                pubnubRequestState.Response.Close();
#endif
                                //asyncRequestState.Response = null;
                                //asyncRequestState.Request = null;
                            }

                        }
                    }
                    ), pubnubRequestState);

                Timer webRequestTimer = new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);
                return taskComplete.Task.Result;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    //pubnubRequestState.Response = ex.Response as HttpWebResponse;
                    using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        //Need to return this response 
#if NET35 || NET40
                        await Task.Factory.StartNew(() => { }).ConfigureAwait(false);
                        string jsonString = streamReader.ReadToEnd();
#else
                        string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON  With POST from WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                        return jsonString;
                    }
                }

                if (ex.Message.IndexOf("The request was aborted: The request was canceled") == -1
                                && ex.Message.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                {
                    taskComplete.TrySetException(ex);
                }
                return "";
            }
            catch (Exception ex)
            {
                taskComplete.TrySetException(ex);
                return "";
            }
        }

        async Task<string> SendRequestAndGetJsonResponseClassicHttpWithPATCH<T>(RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] patchData)
        {
            //LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, Inside SendRequestAndGetJsonResponseClassicHttpWithPATCH", DateTime.Now.ToString(CultureInfo.InvariantCulture)), pubnubConfig.LogVerbosity);
            var taskComplete = new TaskCompletionSource<string>();
            try
            {
                request.Method = "PATCH";
                request.ContentType = "application/json";

                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
#if !NET35 && !NET40 && !NET45 && !NET461
                using (var requestStream = await Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, pubnubRequestState).ConfigureAwait(false))
                {
                    requestStream.Write(patchData, 0, patchData.Length);
                    requestStream.Flush();
                }
#else
                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(patchData, 0, patchData.Length);
                    requestStream.Flush();
                }
#endif

                IAsyncResult asyncResult = request.BeginGetResponse(new AsyncCallback(
                    async (asynchronousResult) => {
                        RequestState<T> asyncRequestState = asynchronousResult.AsyncState as RequestState<T>;
                        HttpWebRequest asyncWebRequest = asyncRequestState.Request as HttpWebRequest;
                        if (asyncWebRequest != null)
                        {
                            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Before EndGetResponse With PATCH ", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                            HttpWebResponse asyncWebResponse = (HttpWebResponse)asyncWebRequest.EndGetResponse(asynchronousResult);
                            stopWatch.Stop();
                            if (TelemetryMgr != null)
                            {
                                await TelemetryMgr.StoreLatency(stopWatch.ElapsedMilliseconds, pubnubRequestState.ResponseType).ConfigureAwait(false);
                            }
                            //asyncRequestState.Response = asyncWebResponse;
                            System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, After EndGetResponse With PATCH ", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                            using (StreamReader streamReader = new StreamReader(asyncWebResponse.GetResponseStream()))
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Inside StreamReader With PATCH ", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                //Need to return this response 
                                string jsonString = streamReader.ReadToEnd();
                                //asyncRequestState.GotJsonResponse = true;

                                System.Diagnostics.Debug.WriteLine(jsonString);
                                System.Diagnostics.Debug.WriteLine("");
                                System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON With PATCH ", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                                taskComplete.TrySetResult(jsonString);
                            }
                            if (asyncRequestState.Response != null)
                            {
#if NET35 || NET40 || NET45 || NET461
                                pubnubRequestState.Response.Close();
#endif
                                //asyncRequestState.Response = null;
                                //asyncRequestState.Request = null;
                            }

                        }
                    }
                    ), pubnubRequestState);

                Timer webRequestTimer = new Timer(OnPubnubWebRequestTimeout<T>, pubnubRequestState, GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000, Timeout.Infinite);
                return taskComplete.Task.Result;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    //pubnubRequestState.Response = ex.Response as HttpWebResponse;
                    using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        //Need to return this response 
#if NET35 || NET40
                        await Task.Factory.StartNew(() => { }).ConfigureAwait(false);
                        string jsonString = streamReader.ReadToEnd();
#else
                        string jsonString = await streamReader.ReadToEndAsync().ConfigureAwait(false);
#endif
                        System.Diagnostics.Debug.WriteLine(jsonString);
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(string.Format("DateTime {0}, Retrieved JSON  With PATCH from WebException response", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                        return jsonString;
                    }
                }

                if (ex.Message.IndexOf("The request was aborted: The request was canceled") == -1
                                && ex.Message.IndexOf("Machine suspend mode enabled. No request will be processed.") == -1)
                {
                    taskComplete.TrySetException(ex);
                }
                return "";
            }
            catch (Exception ex)
            {
                taskComplete.TrySetException(ex);
                return "";
            }
        }

        public HttpWebRequest SetNoCache<T>(HttpWebRequest request)
        {
            request.Headers["Cache-Control"] = "no-cache";
            request.Headers["Pragma"] = "no-cache";

            return request;
        }

        public HttpWebRequest SetProxy<T>(HttpWebRequest request)
        {
#if !NETSTANDARD10
            if (Config.Proxy != null)
            {
                request.Proxy = Config.Proxy;
            }
#endif
            return request;
        }

        public HttpWebRequest SetServicePointSetTcpKeepAlive(HttpWebRequest request)
        {
            //do nothing
            return request;
        }

        public HttpWebRequest SetTimeout<T>(RequestState<T> pubnubRequestState, HttpWebRequest request)
        {
#if NET35 || NET40 || NET45 || NET461
            request.Timeout = GetTimeoutInSecondsForResponseType(pubnubRequestState.ResponseType) * 1000;
#endif
            return request;
        }

        public Task<Tuple<string, PNStatus>> UrlProcessRequest<T>(Uri requestUri, RequestState<T> pubnubRequestState, bool terminateCurrentSubRequest, byte[] postOrPatchData, string contentType)
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<byte[], PNStatus>> UrlProcessRequestForStream<T>(Uri requestUri, RequestState<T> pubnubRequestState, bool terminateCurrentSubRequest, string contentType)
        {
            throw new NotImplementedException();
        }

        protected void OnPubnubWebRequestTimeout<T>(object state, bool timeout)
        {
            //if (timeout && state != null)
            //{
            //    RequestState<T> currentState = state as RequestState<T>;
            //    if (currentState != null)
            //    {
            //        HttpWebRequest request = currentState.Request;
            //        if (request != null)
            //        {
            //            string currentMultiChannel = (currentState.Channels == null) ? "" : string.Join(",", currentState.Channels.OrderBy(x => x).ToArray());
            //            string currentMultiChannelGroup = (currentState.ChannelGroups == null) ? "" : string.Join(",", currentState.ChannelGroups.OrderBy(x => x).ToArray());
            //            LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, OnPubnubWebRequestTimeout: client request timeout reached.Request abort for channel={1} ;channelgroup={2}", DateTime.Now.ToString(CultureInfo.InvariantCulture), currentMultiChannel, currentMultiChannelGroup), pubnubConfig.LogVerbosity);
            //            currentState.Timeout = true;
            //            try
            //            {
            //                request.Abort();
            //            }
            //            catch {  /* ignore */ }
            //        }
            //    }
            //    else
            //    {
            //        LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, OnPubnubWebRequestTimeout: client request timeout reached. However state is unknown", DateTime.Now.ToString(CultureInfo.InvariantCulture)), pubnubConfig.LogVerbosity);
            //    }
            //}
        }

        protected void OnPubnubWebRequestTimeout<T>(System.Object requestState)
        {
            //RequestState<T> currentState = requestState as RequestState<T>;
            //if (currentState != null && currentState.Response == null && currentState.Request != null)
            //{
            //    currentState.Timeout = true;
            //    try
            //    {
            //        currentState.Request.Abort();
            //    }
            //    catch {  /* ignore */ }

            //    LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, **WP7 OnPubnubWebRequestTimeout**", DateTime.Now.ToString(CultureInfo.InvariantCulture)), pubnubConfig.LogVerbosity);

            //    if (currentState.ResponseType != PNOperationType.PNSubscribeOperation
            //        && currentState.ResponseType != PNOperationType.Presence
            //        && currentState.ResponseType != PNOperationType.PNHeartbeatOperation
            //        && currentState.ResponseType != PNOperationType.Leave)
            //    {
            //        PNStatusCategory errorCategory = PNStatusCategory.PNTimeoutCategory;
            //        PNStatus status = new StatusBuilder(pubnubConfig, jsonLib).CreateStatusResponse<T>(currentState.ResponseType, errorCategory, currentState, (int)HttpStatusCode.NotFound, new PNException("Request timeout"));

            //        if (currentState.Channels != null && currentState.Channels.Length > 0)
            //        {
            //            status.AffectedChannels.AddRange(currentState.Channels);
            //        }

            //        if (currentState.ChannelGroups != null && currentState.ChannelGroups.Length > 0)
            //        {
            //            status.AffectedChannels.AddRange(currentState.ChannelGroups);
            //        }

            //        if (currentState.PubnubCallback != null)
            //        {
            //            currentState.PubnubCallback.OnResponse(default(T), status);
            //        }
            //    }
            //}
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

        public List<object> ProcessJsonResponse<T>(RequestState<T> requestState, string json)
        {
            throw new NotImplementedException();
        }
    }
}