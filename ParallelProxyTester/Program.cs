using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelThreading
{
    public class Program
    {
        public static List<string> ProxyList = new List<string>();
        public static List<string> AliveProxies = new List<string>();

        static void Main(string[] args)
        {
            Thread tid1 = new Thread(new ThreadStart(Thread1));
            tid1.Start();
        }

        public static void Thread1()
        {
            PopulateList();
            InvokeParallelProxyTest();
        }

        private static void PopulateList()
        {
            Console.WriteLine("Please enter the path to the .txt proxy list: ");
            var userInput = Console.ReadLine();
            var proxies = File.ReadLines(userInput);
            foreach (string proxy in proxies)
            {
                ProxyList.Add(proxy);
            }
        }


        private static void InvokeParallelProxyTest()
        {
            var watch = Stopwatch.StartNew();
            Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(ProxyList, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, proxy =>
                {
                    SeperateProxies(proxy);
                });
                watch.Stop();
                Export("AliveProxies.txt");
                float elapsedMs = watch.ElapsedMilliseconds;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Finished testing {ProxyList.Count} proxies in: " + elapsedMs + "ms");
                Console.ReadLine();

            });
            Console.ReadLine();
        }

        public static bool TestProxy(string proxyIP, int proxyPort)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.cmyip.org/");
            WebProxy myproxy = new WebProxy(proxyIP, proxyPort);
            myproxy.BypassProxyOnLocal = false;
            request.Proxy = myproxy;
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.95 Safari/537.36";
            request.Timeout = 2000;

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    string Content = sr.ReadToEnd();
                    if (!Content.Contains("CmyIP.org"))
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private static void SeperateProxies(string Proxy)
        {
            string[] proxySplitter = Proxy.Split(':');
            string proxyIP = proxySplitter[0];
            int proxyPort = Convert.ToInt32(proxySplitter[1]);

            if (TestProxy(proxyIP, proxyPort) == true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(proxyIP + ":" + proxyPort);
                AliveProxies.Add(proxyIP + ":" + proxyPort);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Dead Proxy");
            }
        }

        private static void Export(string File)
        {
            using (StreamWriter sw = new StreamWriter(File))
            {
                foreach (string Proxy in AliveProxies)
                {
                    sw.WriteLine(Proxy);
                }
            }
        }
    }
}