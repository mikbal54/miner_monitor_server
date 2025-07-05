using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;
using Nancy;
using Nancy.Hosting.Self;
using System.Collections.Generic;
using Nancy.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Nancy.Conventions;

namespace SimpleWebServer
{


    public class ApplicationBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("view", @"view"));
            base.ConfigureConventions(nancyConventions);
        }
    }

    public class LimitedQueue<T> : Queue<T>
    {
        public int maxNumber = 1000000;

        public void Enqueue(T item)
        {
            if (Count == maxNumber)
                Dequeue();
            base.Enqueue(item);
        }

    }

    public class MinerCommand   
    {
        enum CommandType
        {

        }

    }

    public class MinerInformation
    {

        public string name = "nameless miner";
        public string internet_ip = "no internet ip";
        public string local_ip = "no local ip";
        public int totalHashRate = 0;
        public List<int> hashRates;
        public List<int> temps;
        public long time;

        public MinerInformation()
        {
            hashRates = new List<int>();
            temps = new List<int>();
        }

    }


    public class Miner
    {
        public string name = "noname";
        public List<MinerCommand> waitingCommands;
       // LimitedQueue<MinerInformation> info;
        public LimitedQueue<JObject> info;

        public Miner()
        {
            waitingCommands = new List<MinerCommand>();
            info = new LimitedQueue<JObject>();
        }
    }

    public class Server
    {
        private static Server instance;

        public Dictionary<string, Miner> miners;
        public JObject config;

        private Server() {

            miners = new Dictionary<string, Miner>();

        }

        public static string SHA512(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);

                // Convert to text
                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
                var hashedInputStringBuilder = new System.Text.StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                return hashedInputStringBuilder.ToString();
            }
        }

        public Boolean CheckPassword(string miner, string password)
        {

            if (Server.SHA512(config["password"].ToString()) == password)
                    return true;
            else
                return false;
        }

        public static Server Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Server();
                }
                return instance;
            }
        }
    }

    public class SimpleModule : Nancy.NancyModule
    {
        

        public SimpleModule()
        {

            Get["/"] = _ => View["index"];
            Post["/minerInfoUpload"] = paramaters =>
            {

                JObject minerInfo = JObject.Parse(this.Request.Body.AsString());
                if (!Server.Instance.CheckPassword(minerInfo["name"].ToString(), minerInfo["password"].ToString()))
                    return "Wrong password or miner name!";

                Miner miner = null;
                Server.Instance.miners.TryGetValue(minerInfo["name"].ToString(), out miner);
                if (miner == null)
                {
                    miner = new Miner();
                    miner.name = minerInfo["name"].ToString();
                    Server.Instance.miners.Add(minerInfo["name"].ToString(), miner);
                }
                miner.info.Enqueue(minerInfo);

                Console.WriteLine("Miner Info: " + minerInfo.ToString());

                return miner.name +" Recorded";

            };

            Get["/minerStats/{seconds}"] = param =>
            {
                List<JObject> answers = new List<JObject>();


                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan diff = DateTime.Now.ToUniversalTime() - origin;
                double now = Math.Floor(diff.TotalSeconds);
                int seconds = param["seconds"];
                foreach (var a in Server.Instance.miners)
                {

                    var res = from item in a.Value.info
                              where item["time"].Value<double>() > (now - seconds)
                              select item;

                    answers.AddRange(res.ToList());
                    
                }

                var array = JArray.FromObject(answers);

                return array.ToString();
            };
        }
    }

    class Program
    {

        static void Main(string[] args)
        {

            using (StreamReader r = new StreamReader("config.json"))
            {
                string json = r.ReadToEnd();
                Server.Instance.config = JObject.Parse(json);
            }

            HostConfiguration hostConf = new HostConfiguration();
            hostConf.UrlReservations = new UrlReservations() { CreateAutomatically = true };
            hostConf.RewriteLocalhost = true;
            string address = "http://localhost:" + Server.Instance.config["port"].ToString();
            var apiHost = new NancyHost(hostConf, new Uri(address));

            apiHost.Start();
            Console.WriteLine("Server started on: " + address);
            Console.ReadKey();

            apiHost.Stop();
        }




    }
}