using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PubnubApi
{
    public interface IPNDataService
    {
        PNConfiguration Config { get; }
        Task<Tuple<string, PNStatus>> UrlProcessRequest<T>(Uri requestUri, RequestState<T> pubnubRequestState, bool terminateCurrentSubRequest, byte[] postOrPatchData, string contentType);
        Task<Tuple<byte[], PNStatus>> UrlProcessRequestForStream<T>(Uri requestUri, RequestState<T> pubnubRequestState, bool terminateCurrentSubRequest, string contentType);
        List<object> ProcessJsonResponse<T>(RequestState<T> requestState, string json);
    }
}
