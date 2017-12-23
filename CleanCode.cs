using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

public class Program
{
    static void Main(string[] args)
    {
        var runner = new ProxyRunner();
        var task = runner.Start();
        task.GetAwaiter().GetResult();

        Console.ForegroundColor = ConsoleColor.White;
        Console.ReadLine();
    }
}

public class ProxyRunner
{
    private const string EXPORT_FILE = "AliveProxies.txt";
    private List<Proxy> _proxies = new List<Proxy>();

    public async Task Start()
    {
        var stopwatch = new Stopwatch();

        GetProxies();

        stopwatch.Start();

        await ValidateProxies();

        stopwatch.Stop();

        Console.WriteLine($"Finished testing {_proxies.Count} proxies in: " + stopwatch.ElapsedMilliseconds + "ms");
        Console.WriteLine("Exporting verified proxies");
        ExportVerifiedProxies();

    }

    private void GetProxies()
    {
        Console.WriteLine("Please enter the path to the .txt proxy list: ");

        var userInput = Console.ReadLine();
        var proxies = File.ReadLines(userInput);
        _proxies = proxies.Select(x => new Proxy(x)).ToList();
    }


    private async Task ValidateProxies()
    {
        var tasks = _proxies.Select(x => Task.Run(() => TextProxy(x)));

        await Task.WhenAll(tasks);
    }

    private void TextProxy(Proxy proxy)
    {
        if (TestProxy(proxy.Ip, proxy.Port) == true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(proxy.Address);
            proxy.Validate();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Dead Proxy");
        }
    }

    public bool TestProxy(string proxyIP, int proxyPort)
    {
        const string WEB_REQUEST_URL = "http://www.cmyip.org/";
        const string REQUEST_METHOD = "GET";
        const string REQUEST_AGENT = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.95 Safari/537.36";

        const int REQUEST_TIMEOUT = 2000;

        var request = (HttpWebRequest)WebRequest.Create(WEB_REQUEST_URL);
        var proxy = new WebProxy(proxyIP, proxyPort);

        proxy.BypassProxyOnLocal = false;
        request.Proxy = proxy;
        request.Method = REQUEST_METHOD;
        request.UserAgent = REQUEST_AGENT;
        request.Timeout = REQUEST_TIMEOUT;

        try
        {
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                string Content = sr.ReadToEnd();
                if (Content.Contains("CmyIP.org"))
                    return true;
            }
        }
        catch (Exception)
        {
            return false;
        }

        return false;
    }

    private void ExportVerifiedProxies()
    {
        var validProxies = _proxies.Where(x => x.IsValid).Select(x => x.Address);
        System.IO.File.WriteAllLines(EXPORT_FILE, validProxies);
    }
}

public class Proxy
{
    public Proxy(string address)
    {
        Address = address;

        var split = address.Split(':');
        Ip = split[0];
        Port = int.Parse(split[1]);
    }

    public string Address { get; set; }
    public bool IsValid { get; private set; }
    public string Ip { get; }
    public int Port { get; }

    public void Validate() => IsValid = true;
}

