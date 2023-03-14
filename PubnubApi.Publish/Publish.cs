using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    [Export(typeof(IPublishOperation))]
    public class Publish : IPublishOperation
    {
        [Import(typeof(IPubnubHttp))]
        private IPubnubHttp _pubnubHttp;

        private object msg;
        private string channelName = "";
        private bool storeInHistory = true;
        private bool httpPost;
        private Dictionary<string, object> userMetadata;
        private int ttl = -1;
        //private PNCallback<PNPublishResult> savedCallback;
        private bool syncRequest;
        private Dictionary<string, object> queryParam;

        public Publish()
        {
            var aggregateCatalog = new AggregateCatalog();
            //aggregateCatalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
#if NET46_OR_GREATER
            aggregateCatalog.Catalogs.Add(new DirectoryCatalog(AppContext.BaseDirectory));
#else
            aggregateCatalog.Catalogs.Add(new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory));
#endif

            var compositionContainer = new CompositionContainer(aggregateCatalog);

            var compositionBatch = new CompositionBatch();
            compositionBatch.AddPart(this);


            compositionContainer.Compose(compositionBatch);

            

        }
        public PNConfiguration Config 
        { 
            get;
            set; 
        }
        public IJsonPluggableLibrary JsonLibrary 
        {  
            get; 
            set; 
        }
        public IPubnubLog PubnubLog 
        { 
            get; 
            set; 
        }

        public IPublishOperation Channel(string channelName)
        {
            this.channelName = channelName;
            return this;
        }

        public IPublishOperation Message(object message)
        {
            this.msg = message;
            return this;
        }

        public IPublishOperation Meta(Dictionary<string, object> metadata)
        {
            this.userMetadata = metadata;
            return this;
        }

        public IPublishOperation QueryParam(Dictionary<string, object> customQueryParam)
        {
            this.queryParam = customQueryParam;
            return this;
        }

        public IPublishOperation ShouldStore(bool store)
        {
            this.storeInHistory = store;
            return this;
        }

        public IPublishOperation Ttl(int ttl)
        {
            this.ttl = ttl;
            return this;
        }

        public IPublishOperation UsePOST(bool post)
        {
            this.httpPost = post;
            return this;
        }

        public void Execute()
        {

        }

        public async Task<PNResult<PNPublishResult>> ExecuteAsync()
        {
            return await PublishInit(this.channelName, this.msg, this.storeInHistory, this.ttl, this.userMetadata, this.queryParam).ConfigureAwait(false);
        }

        internal async Task<PNResult<PNPublishResult>> PublishInit(string channel, object message, bool storeInHistory, int ttl, Dictionary<string, object> metaData, Dictionary<string, object> externalQueryParam)
        {
            PNResult<PNPublishResult> ret = new PNResult<PNPublishResult>();
            if (_pubnubHttp != null)
            {
                _pubnubHttp.Config = Config;
            }

            if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(channel.Trim()) || message == null)
            {
                PNStatus errStatus = new PNStatus();
                errStatus.Error = true;
                errStatus.ErrorData = new PNErrorData("Missing Channel or Message", new ArgumentException("Missing Channel or Message"));
                ret.Status = errStatus;
                return ret;
            }

            if (string.IsNullOrEmpty(Config.PublishKey) || string.IsNullOrEmpty(Config.PublishKey.Trim()) || Config.PublishKey.Length <= 0)
            {
                PNStatus errStatus = new PNStatus();
                errStatus.Error = true;
                errStatus.ErrorData = new PNErrorData("Invalid publish key", new MissingMemberException("Invalid publish key"));
                ret.Status = errStatus;
                return ret;
            }

            RequestState<PNPublishResult> requestState = new RequestState<PNPublishResult>();
            try
            {
                string requestMethodName = (this.httpPost) ? "POST" : "GET";
                UrlRequestBuilder urlBuilder = new UrlRequestBuilder(Config, JsonLibrary, PubnubLog);

                Uri request = urlBuilder.BuildPublishRequest(requestMethodName, "", channel, message, storeInHistory, ttl, metaData, null, externalQueryParam);

                requestState.Channels = new[] { channel };
                requestState.ResponseType = PNOperationType.PNPublishOperation;
                requestState.Reconnect = false;
                requestState.EndPointOperation = this;

                Tuple<string, PNStatus> JsonAndStatusTuple;
                //IPNDataService dataService = null;

                if (this.httpPost)
                {
                    requestState.UsePostMethod = true;
                    string postMessage = JsonEncodePublishMsg(message);
                    byte[] postData = Encoding.UTF8.GetBytes(postMessage);
                    JsonAndStatusTuple = await _pubnubHttp.UrlProcessRequest(request, requestState, false, postData, "").ConfigureAwait(false);
                }
                else
                {
                    JsonAndStatusTuple = await _pubnubHttp.UrlProcessRequest(request, requestState, false, null, "").ConfigureAwait(false);
                }
                ret.Status = JsonAndStatusTuple.Item2;
                string json = JsonAndStatusTuple.Item1;

                if (!string.IsNullOrEmpty(json) && ret.Status.StatusCode == 200)
                {
                    List<object> result = ProcessJsonResponse(json);

                    if (result != null && result.Count >= 3)
                    {
                        int publishStatus;
                        Int32.TryParse(result[0].ToString(), out publishStatus);
                        if (publishStatus == 1)
                        {
                            ResponseBuilder responseBuilder = new ResponseBuilder(Config, JsonLibrary, PubnubLog);
                            PNPublishResult responseResult = responseBuilder.JsonToObject<PNPublishResult>(result);
                            if (responseResult != null)
                            {
                                ret.Result = responseResult;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO:PANDU
                //int statusCode = PNStatusCodeHelper.GetHttpStatusCode(ex.ToString());
                //PNStatusCategory category = PNStatusCategoryHelper.GetPNStatusCategory(statusCode, ex.ToString());
                //PNStatus status = new StatusBuilder(config, jsonLibrary).CreateStatusResponse(PNOperationType.PNPublishOperation, category, requestState, statusCode, new PNException(ex.ToString()));
                //ret.Status = status;
            }

            return ret;
        }

        private string JsonEncodePublishMsg(object originalMessage)
        {
            string message = JsonLibrary.SerializeToJsonString(originalMessage);

            if (Config.CipherKey.Length > 0)
            {
                PubnubCrypto aes = new PubnubCrypto(Config.CipherKey, Config, PubnubLog, null);
                string encryptMessage = aes.Encrypt(message);
                message = JsonLibrary.SerializeToJsonString(encryptMessage);
            }

            return message;
        }

        private List<object> ProcessJsonResponse(string jsonString)
        {
            List<object> result = new List<object>();
            if (!string.IsNullOrEmpty(jsonString))
            {
                object deserializedResult = JsonLibrary.DeserializeToObject(jsonString);
                List<object> result1 = ((IEnumerable)deserializedResult).Cast<object>().ToList();

                if (result1 != null && result1.Count > 0)
                {
                    result = result1;
                }
                result.Add(channelName);
            }
            return result;
        }
    }
}