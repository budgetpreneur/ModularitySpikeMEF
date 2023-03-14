using System.Reflection;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System;

namespace PubnubApi
{
    public class Pubnub
    {
        //private IPublishOperation _publishOperation;// = new List<IPublishOperation>();


        private readonly string savedSdkVerion;

        public string InstanceId { get; private set; }
        public static string Version { get; private set; }

        //[Import(typeof(IPubnubLog))]
        //public IPubnubLog PubnubLog;

        [Import(typeof(IJsonPluggableLibrary))]
        public IJsonPluggableLibrary JsonLibrary;

        [Import(typeof(IPublishOperation))]
        public IPublishOperation Publish;
        static Pubnub()
        {
#if NET35 || NET40
            var assemblyVersion = typeof(Pubnub).Assembly.GetName().Version;
#else
            var assembly = typeof(Pubnub).GetTypeInfo().Assembly;
            var assemblyName = new AssemblyName(assembly.FullName);
            string assemblyVersion = assemblyName.Version.ToString();
#endif
            Version = string.Format("{0}CSharp{1}", PNPlatform.Get(), assemblyVersion);
        }

        public Pubnub(PNConfiguration config)
        {
            savedSdkVerion = Version;
            InstanceId = Guid.NewGuid().ToString();
            CheckRequiredConfigValues(config);
            if (config != null && config.PresenceTimeout < 20)
            {
                config.PresenceTimeout = 20;
            }
            if (config != null && (string.IsNullOrEmpty(config.Uuid) || string.IsNullOrEmpty(config.Uuid.Trim())))
            {
                throw new MissingMemberException("PNConfiguration.Uuid is required to use the SDK");
            }
            try
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
            catch (CompositionException cex){
                var loadException = cex.Errors[0];

                // as the error said,
                // "Retrieve the LoaderExceptions property for more information"
                var cause = loadException.ToString();

                // print, log or extract the information in some other way 
                System.Diagnostics.Debug.WriteLine(cause);
            }

            Publish.Config = config;
            Publish.JsonLibrary = JsonLibrary;
            //Publish.PubnubLog = PubnubLog;

        }

        private void CheckRequiredConfigValues(PNConfiguration pubnubConfig)
        {
            if (pubnubConfig != null)
            {
                if (string.IsNullOrEmpty(pubnubConfig.SubscribeKey))
                {
                    pubnubConfig.SubscribeKey = "";
                }

                if (string.IsNullOrEmpty(pubnubConfig.PublishKey))
                {
                    pubnubConfig.PublishKey = "";
                }

                if (string.IsNullOrEmpty(pubnubConfig.SecretKey))
                {
                    pubnubConfig.SecretKey = "";
                }

                if (string.IsNullOrEmpty(pubnubConfig.CipherKey))
                {
                    pubnubConfig.CipherKey = "";
                }
            }
        }
    }
}