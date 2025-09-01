// PoseSequences.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using SDKHrobot;

namespace IAOAP
{
    /// <summary>
    /// 執行機器人姿態序列：依優先順序(A→B→C→D)觸發單一區域動作，
    /// 動作結束後暫停1秒，整體運行時長15分鐘
    /// </summary>
    public static class PoseSequences
    {
        /// <summary>
        /// 供 EntryPoint callback 呼叫，以通知 ptp_axis 動作完成
        /// </summary>
        public static readonly AutoResetEvent MotionDoneEvent = new AutoResetEvent(false);

        /// <summary>
        /// 各情境對應的姿態序列
        /// </summary>
        public static readonly Dictionary<string, List<double[]>> Sequences = new Dictionary<string, List<double[]>>

        {
            ["1-A"] = new List<double[]> {
                new double[]{ -165,   0,    0,    0,   0,   0 },
                new double[]{ -165, 19.8,   0,    0,   0,   0 },
                new double[]{ -165,-23.2,   0,    0,   0,   0 },
                new double[]{   0,   0,    0,    0,   0,   0 }
            },
            ["1-B"] = new List<double[]> {
                new double[]{  77.3,  -6,    0, -29.8,  0,   0 },
                new double[]{  77.3,  -6,    0,  47.1,  0,   0 },
                new double[]{  77.3,  -6,    0, -31.6,  0,   0 },
                new double[]{  77.3,  -6,    0,  36.2,  0,   0 },
                new double[]{  77.3,  -6,    0,  36.2,  5.1, 0 },
                new double[]{  77.3,  -6,    0,  36.2,-35.9, 0 }
            },
            ["1-C"] = new List<double[]> {
                new double[]{  -6.2, -16.4,  0, -53.3, -26.6, 0 },
                new double[]{   4.3, -16.4,  0,  31.9, -21.1, 0 },
                new double[]{  -9.9, -16.4,  0, -39.5, -21.1, 0 },
                new double[]{  12.4, -16.4,  0,  24.6, -21.1, 0 }
            },
            ["1-D"] = new List<double[]> {
                new double[]{ -57.5,  72.6, -50,    0,    0,   0 },
                new double[]{ -57.5,  72.6, -46.7,  0,    0,   0 },
                new double[]{ -57.5,  72.6, -55,    0,  -19.3, 0 },
                new double[]{ -57.5,  72.6, -41,    0,  -19.3, 0 },
                new double[]{ -57.5,  72.6, -55,    0,  -19.3, 0 }
            },
            ["2-A"] = new List<double[]> {
                new double[]{ 165,    0,    0,    0,    0,   0 },
                new double[]{ -165,   0,    0,    0,    0,   0 },
                new double[]{ -165, -10.8, -16.1, -37.4, 0,   0 },
                new double[]{ -165,  15.3, -16.1, -37.4, 0,   0 },
                new double[]{ -165,  -6.8, -16.1, -37.4, 0,   0 },
                new double[]{ 165,   -6.8, -16.1, -37.4, 0,   0 }
            },
            ["2-B"] = new List<double[]> {
                new double[]{ 165,    0,    0,    0,    0,   0 },
                new double[]{ -165,   0,    0,    0,    0,   0 },
                new double[]{ -165, -10.8, -16.1, -37.4, 0,   0 },
                new double[]{ -165,  15.3, -16.1, -37.4, 0,   0 },
                new double[]{ -165,  15.3, -16.1, -37.4, 0,   0 },
                new double[]{ -165,  -6.8, -16.1, -37.4, 0,   0 },
                new double[]{ 165,   -6.8, -16.1, -37.4, 0,   0 }
            },
            ["2-C"] = new List<double[]> {
                new double[]{   0,   25.8, 33.4,   0,    0, 0 },
                new double[]{ -54.7,25.8, 33.4,   0,    0, 0 },
                new double[]{  60.5,25.8, 33.4,   0,    0, 0 },
                new double[]{ -48.6,25.8, 33.4,   0,    0, 0 },
                new double[]{   0,    1, -20.5,   0,    0, 0 }
            },
            ["2-D"] = new List<double[]> {
                new double[]{  -91.1, 67,  -21.7, -10.7,-46.7,0 },
                new double[]{   -40,  67,  -55,   -10.7,-46.7,0 },
                new double[]{ -108.7, 67,  -32,     30.1,-46.7,0 },
                new double[]{ -108.7,22.2,   7.6,  -19.3,-48.1,0 },
                new double[]{  -73.2, 2.1, -22.3,  -19.3,-48.1,0 }
            },
            ["3-A"] = new List<double[]> {
                new double[]{ -165,   0,  36.6,   0,   0, 0 },
                new double[]{ -165,   0, -14.7,   0,   0, 0 },
                new double[]{ -165, 70.3,-55,     0,   0, 0 },
                new double[]{ -165,  2.6,18.2,   0,   0, 0 },
                new double[]{ -165,  2.6,18.2, 36.5,  0, 0 },
                new double[]{ -165,  2.6,18.2,-30.9,  0, 0 }
            },
            ["3-B"] = new List<double[]> {
                new double[]{  93.9, 14,    0,  38.9,   0,   0 },
                new double[]{  93.9, 14,    0, -49,     0,   0 },
                new double[]{  93.9, 14,    0,  48,     0,   0 },
                new double[]{  93.9, 14,    0, -37.9,   0,   0 },
                new double[]{  93.9, 33.3,-48.6, 35.4, -5.1, 0 }
            },
            ["3-C"] = new List<double[]> {
                new double[]{  -12.3,  2,    0,    0, -52.2, 0 },
                new double[]{  -12.3,  2,    0,    0, -52.2, 0 },
                new double[]{  -12.3,  2,    0,    0, -52.2, 0 },
                new double[]{  -12.3,  2, -18.7,    0, -52.2, 0 },
                new double[]{  -12.3,  2,    0.4,  0, -52.2, 0 },
                new double[]{  -12.3,  2, -17.2,    0, -52.2, 0 },
                new double[]{   22.7,  2,    1.8,  0, -52.2, 0 }
            },
            ["3-D"] = new List<double[]> {
                new double[]{  -77.5, 53.8, -36.4,   0,   0, 0 },
                new double[]{  -35.2, 17.7, -36.4,   0,   0, 0 },
                new double[]{  -97.5, 46.4, -36.4, -46.9, 0,   0 },
                new double[]{  -37.9, 57,   -36.4,  24.8, 0,   0 }
            },
            ["more-than-3"] = new List<double[]> {
                new double[]{ -165,  34, -49.7,   0,   0,  0 },
                new double[]{  165,  34, -49.7,   0,   0,  0 },
                new double[]{  -22.8,34, -49.7, 36.7, 0,   0 },
                new double[]{   32.6,34, -49.7,-44.3, 0,   0 },
                new double[]{  -19.9,49.8,-33.2,   0,   0,  0 }
            }
        }
;

        /// <summary>
        /// 執行該區域所有姿態，並於每個動作後停 1 秒
        /// </summary>
        private static void ExecuteZone(List<double[]> seqList, int handle)
        {
            foreach (var joints in seqList)
            {
                var jointStr = string.Join(",", joints);
                Console.WriteLine($"ExecuteZone: 呼叫 ptp_axis, joints=[{jointStr}]");
                MotionDoneEvent.Reset();

                int ret = HRobot.ptp_axis(handle, 0, joints);
                if (ret != 0)
                {
                    Console.WriteLine($"ptp_axis 失敗，錯誤碼：{ret}");
                    // 避免卡死，錯誤時強制放行
                    MotionDoneEvent.Set();
                    continue;
                }

                // 等候 callback 通知完成
                MotionDoneEvent.WaitOne();
                Console.WriteLine("ExecuteZone: 動作完成");
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// 依優先順序(A→B→C→D)執行單一區域的姿態序列
        /// </summary>
        private static void ExecuteSequences(List<Region> regions, int handle)
        {
            var map = regions.ToDictionary(r => r.ID);
            foreach (var id in new[] { "1", "2", "3", "4" })
            {
                if (!map.TryGetValue(id, out var reg)) continue;
                var zoneData = reg.Zone?.FirstOrDefault();
                if (zoneData == null || zoneData.PeopleCount == 0) continue;

                string zone = id switch { "1" => "A", "2" => "B", "3" => "C", "4" => "D" };
                int count = zoneData.PeopleCount;
                string key = count > 3 ? "more-than-3" : $"{count}-{zone}";



                string SoundPC_IP = "192.168.1.154";// "10.13.10.131";// "192.168.1.154";
                int SoundPC_port = 8082;
                string LightPC_IP = "192.168.1.99";
                int LightPC_port = 9897;

                switch (count)
                {
                    case 1:
                        switch (zone)
                        {
                            case "A":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "C");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "C");
                                break;
                            case "B":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "D");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "D");
                                break;
                            case "C":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "E");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "E");
                                break;
                            case "D":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "F");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "F");
                                break;
                        }
                        break;
                    case 2:
                        switch (zone)
                        {
                            case "A":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "G");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "G");
                                break;
                            case "B":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "H");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "H");
                                break;
                            case "C":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "I");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "I");
                                break;
                            case "D":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "J");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "J");
                                break;
                        }
                        break;
                    case 3:
                        switch (zone)
                        {
                            case "A":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "K");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "K");
                                break;
                            case "B":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "L");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "L");
                                break;
                            case "C":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "M");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "M");
                                break;
                            case "D":
                                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "N");
                                UdpSender.SendMessage(LightPC_IP, LightPC_port, "N");
                                break;
                        }
                        break;
                    default:
                        UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "O");
                        UdpSender.SendMessage(LightPC_IP, LightPC_port, "O");
                        break;
                }


            if (Sequences.TryGetValue(key, out var seqList))
                {
                    Console.WriteLine($"ExecuteSequences: 模式 {key}，觸發 {seqList.Count} 組姿態");
                    ExecuteZone(seqList, handle);
                }
                break;
            }
        }

        /// <summary>
        /// 啟動 UDP 監聽，接收 JSON 後依條件執行動作，
        /// 動作期間暫停接收，運行15分鐘後自動停止
        /// </summary>
        public static void StartListener(int port, int handle)
        {
            var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            Console.WriteLine($"StartListener: 開始監聽 UDP (Port {port})，運行時長 15 分鐘");

            var startTime = DateTime.Now;
            while (DateTime.Now - startTime < TimeSpan.FromMinutes(15))
            {
                // 清空排隊緩衝
                var flushEP = new IPEndPoint(IPAddress.Any, 0);
                while (udp.Available > 0) udp.Receive(ref flushEP);

                Console.WriteLine("StartListener: 等待下一筆 UDP 訊息...");
                var remoteEP = new IPEndPoint(IPAddress.Any, 0);
                var buffer = udp.Receive(ref remoteEP);
                string json = Encoding.UTF8.GetString(buffer);
                Console.WriteLine($"StartListener: 收到 UDP 資料: {json}");


            try
                {
                    var regions = JsonConvert.DeserializeObject<List<Region>>(json);
                    ExecuteSequences(regions, handle);
                    Console.WriteLine("執行完成，準備接收UDP");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"StartListener 錯誤: {ex.Message}");
                }
            }

            Console.WriteLine("StartListener: 15 分鐘到達，停止監聽並關閉 UdpClient");
            udp.Close();
        }
    }
}

