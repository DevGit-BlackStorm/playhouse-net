using System.Net.Sockets;
using System.Net;

namespace PlayHouse.Communicator;
public static class IpFinder
{
    public static string FindLocalIp()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
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
        List<string> urlList = new List<string>
        {
            "http://checkip.amazonaws.com/",
            "https://ipv4.icanhazip.com/",
            "http://myexternalip.com/raw",
            "http://ipecho.net/plain"
        };

        foreach (string urlString in urlList)
        {
            string checkPublicIp = CheckPublicIp(urlString);
            if (IPAddress.TryParse(checkPublicIp, out IPAddress? ip) && ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return checkPublicIp;
            }
        }

        return FindLocalIp();
    }

    public static int FindFreePort()
    {
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            socket.Listen(1);
            int port = ((IPEndPoint)socket.LocalEndPoint!).Port;
            socket.Close();
            return port;
        }
    }

    private static string CheckPublicIp(string urlString)
    {
        using (HttpClient client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(5);

            try
            {
                HttpResponseMessage response = client.GetAsync(urlString).Result;
                response.EnsureSuccessStatusCode();

                string content = response.Content.ReadAsStringAsync().Result;
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
