using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubnubApi;

namespace IntroductionToMEF
{
    public class Program
    {
        static public void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
        static public async Task MainAsync()
        {
            PNConfiguration config = new PNConfiguration("etestuuid");
            config.SubscribeKey = "demo";
            config.PublishKey = "demo";
            Pubnub pn = new Pubnub(config);
            PNResult<PNPublishResult> publishResponse = await pn.Publish.Channel("test").Message("helloworld").ExecuteAsync();
            PNPublishResult publishResult = publishResponse.Result;
            PNStatus publishStatus = publishResponse.Status;
            if (publishResult != null && publishStatus.StatusCode == 200)
            {
                Console.WriteLine(pn.JsonLibrary.SerializeToJsonString(publishResponse.Result));
            }
            //PluginManager pluginManager = new PluginManager();

            //Console.WriteLine(string.Format("Number of Loaded Rules: {0}", pluginManager.Rules.Count));

            //pluginManager.SetupManager();

            //Console.WriteLine(string.Format("Number of Loaded Rules: {0}", pluginManager.Rules.Count));

            //foreach(var rule in pluginManager.Rules)
            //{
            //    Console.WriteLine(string.Format("Found Rule {0}", rule.Name));
            //    rule.DoSomething1();
            //}

            //Console.WriteLine(string.Format("Number of Publish Ops: {0}", pluginManager.PublishOperations.Count));

            Console.ReadLine();


        }
    }
}
