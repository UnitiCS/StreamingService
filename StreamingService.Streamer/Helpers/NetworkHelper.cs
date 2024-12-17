using System.Net.NetworkInformation;
using System.Net;

namespace StreamingService.Streamer.Helpers
{
    public static class NetworkHelper
    {
        public static bool IsPortAvailable(int port)
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            return !tcpConnInfoArray.Any(endpoint => endpoint.Port == port);
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        public static async Task<bool> TestNetworkSpeed()
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    var startTime = DateTime.Now;
                    await client.DownloadDataTaskAsync("http://www.google.com");
                    var endTime = DateTime.Now;

                    var duration = (endTime - startTime).TotalSeconds;
                    return duration < 1.0; // Если время ответа меньше 1 секунды, считаем соединение хорошим
                }
            }
            catch
            {
                return false;
            }
        }
    }
}