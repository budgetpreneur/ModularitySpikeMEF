using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace PubnubApi
{
    public interface IPubnubHttp
    {
        PNConfiguration Config { get; set; }

        ITelemetry TelemetryMgr { get; set; }

        Task<Tuple<string, PNStatus>> UrlProcessRequest<T>(Uri requestUri, RequestState<T> pubnubRequestState, bool terminateCurrentSubRequest, byte[] postOrPatchData, string contentType);
        Task<Tuple<byte[], PNStatus>> UrlProcessRequestForStream<T>(Uri requestUri, RequestState<T> pubnubRequestState, bool terminateCurrentSubRequest, string contentType);
        //List<object> ProcessJsonResponse<T>(RequestState<T> requestState, string json);

        HttpWebRequest SetProxy<T>(HttpWebRequest request);

        HttpWebRequest SetTimeout<T>(RequestState<T> pubnubRequestState, HttpWebRequest request);

        HttpWebRequest SetNoCache<T>(HttpWebRequest request);

        HttpWebRequest SetServicePointSetTcpKeepAlive(HttpWebRequest request);

        Task<Tuple<string, int>> SendRequestAndGetJsonResponse<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request);

        Task<Tuple<string, int>> SendRequestAndGetJsonResponseWithPOST<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] postData, string contentType);

        Task<Tuple<string, int>> SendRequestAndGetJsonResponseWithPATCH<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request, byte[] patchData);

        Task<byte[]> SendRequestAndGetStreamResponse<T>(Uri requestUri, RequestState<T> pubnubRequestState, HttpWebRequest request);
    }
}
