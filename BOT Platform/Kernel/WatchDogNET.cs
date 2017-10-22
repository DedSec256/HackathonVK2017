using System.Net;
using System.IO;
using System.Net.NetworkInformation;
using System;

namespace BOT_Platform.Kernel
{
    public static class ConnectivityChecker
    {
        public static (bool status, string info) CheckConnection(string url)
        {
            IPStatus status = IPStatus.TimedOut;
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(@"vk.com");
                status = reply.Status;
            }
           
            catch
            {
                return (false, 
                    "[NET_ERROR " + DateTime.Now.ToLongTimeString() + $"] Непредвиденная ошибка при попытке соединения с {url}\n");
            }
            if (status != IPStatus.Success)
            {
                return (false,
                    "[NET_ERROR " + DateTime.Now.ToLongTimeString() + $"] Не удалось подключиться к {url}: " + status.ToString() + "\n");
            }
            else
            {
                return (true,
                    "[NET_INFO " + DateTime.Now.ToLongTimeString() + $"] Соединение с {url} установлено (SUCCSSES).\n");
            }
        }
    }
}