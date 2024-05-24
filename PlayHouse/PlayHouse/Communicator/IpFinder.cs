using System.Net;
using System.Net.Sockets;

namespace PlayHouse.Communicator;

public static class IpFinder
{
    public static string FindLocalIp()
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(new IPEndPoint(Dns.GetHostAddresses("google.com")[0], 80));
                return ((IPEndPoint)socket.LocalEndPoint!).Address.ToString();
            }
        }
        catch (Exception)
        {
            return Dns.GetHostAddresses(Dns.GetHostName())[0].ToString();
        }
    }

    public static string FindPublicIp()
    {
        var urlList = new List<string>
        {
            "http://checkip.amazonaws.com/",
            "https://ipv4.icanhazip.com/",
            "http://myexternalip.com/raw",
            "http://ipecho.net/plain"
        };

        foreach (var urlString in urlList)
        {
            var checkPublicIp = CheckPublicIp(urlString);
            if (IPAddress.TryParse(checkPublicIp, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return checkPublicIp;
            }
        }

        return FindLocalIp();
    }

    public static int FindFreePort()
    {
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            socket.Listen(1);
            var port = ((IPEndPoint)socket.LocalEndPoint!).Port;
            socket.Close();
            return port;
        }
    }

    private static string CheckPublicIp(string urlString)
    {
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(5);

            try
            {
                var response = client.GetAsync(urlString).Result;
                response.EnsureSuccessStatusCode();

                var content = response.Content.ReadAsStringAsync().Result;
                return content;
            }
            catch (HttpRequestException ex)
            {
                // Handle any exceptions that occur
                return $"Error: {ex.Message}";
            }
        }
    }
}