using System;
using System.Net.Sockets;
using System.Text;

namespace IAOAP
{
    public static class UdpSender
    {


        public static void SendMessage(string ipAddress, int port, string message)
        {
            try
            {
                using (var udpClient = new UdpClient())
                {
                    var data = Encoding.UTF8.GetBytes(message);
                    udpClient.Send(data, data.Length, ipAddress, port);
                    Console.WriteLine($"UDP: 成功發送 '{message}' 至 {ipAddress}:{port}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP 傳送失敗: {ex.Message}");
            }
        }
    }
}
//using System;
//using System.Net.Sockets;
//using System.Text;
//using SharpOSC; // 引入 SharpOSC 命名空間

//namespace IAOAP
//{
//    public static class UdpSender
//    {
//        // 舊的純文字傳送方法（可以保留）
//        public static void SendMessage(string ipAddress, int port, string message)
//        {
//            try
//            {
//                using (var udpClient = new UdpClient())
//                {
//                    var data = Encoding.UTF8.GetBytes(message);
//                    udpClient.Send(data, data.Length, ipAddress, port);
//                    Console.WriteLine($"UDP: 成功發送 '{message}' 至 {ipAddress}:{port}");
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"UDP 傳送失敗: {ex.Message}");
//            }
//        }

//        // 新增一個發送 OSC 訊息的方法
//        public static void SendOscMessage(string ipAddress, int port, string address, params object[] arguments)
//        {
//            try
//            {
//                // 建立一個 OSC 訊息
//                var message = new OscMessage(address, arguments);

//                // 建立一個 UDP 發送器
//                var sender = new UDPSender(ipAddress, port);

//                // 發送 OSC 訊息
//                sender.Send(message);

//                Console.WriteLine($"OSC: 成功發送 '{address}' 訊息至 {ipAddress}:{port}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"OSC 傳送失敗: {ex.Message}");
//            }
//        }
//    }
//}