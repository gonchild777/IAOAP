
using System;
using System.Threading;
using System.Threading.Tasks;
using SDKHrobot;
using IAOAP;
using System.Threading.Tasks;

namespace IAOAP
{
    public class EntryPoint
    {
        public static string SoundPC_IP = "192.168.1.154";// "10.13.10.131";//"192.168.1.154";
        public static int SoundPC_port = 8082;
        public static string LightPC_IP = "192.168.1.99";
        public static int LightPC_port = 9897;
        public const int ListenPort = 9999;

        // 防止 callback delegate 被 GC，需要保留參考
        private static HRobot.CallBackFun RobotCallback;
        // 機器人連線句柄
        private static int RobotHandle;

        public static void Main(string[] args)
        {
            Console.WriteLine("EntryPoint: 開始手臂連線測試");
            //test zone
            //OscManager.Initialize(ListenPort, SoundPC_IP, SoundPC_port);


            //int j = 0;
            //while (true)
            //{
            //    //OscManager.SendOsc("/mode/B", "Start"); // 發送 OSC 訊息

            //    UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "A");
            //    UdpSender.SendMessage(LightPC_IP, LightPC_port, "A");

            //    Thread.Sleep(5000);
            //}
                //    UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "B");
                //    UdpSender.SendMessage(LightPC_IP, LightPC_port, "B");

                //    Thread.Sleep(2000);
                //    UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "C");
                //    UdpSender.SendMessage(LightPC_IP, LightPC_port, "C");
                //    Thread.Sleep(2000);

                //    UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "D");
                //    UdpSender.SendMessage(LightPC_IP, LightPC_port, "D");
                //    Thread.Sleep(2000);
                //    UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "E");
                //    UdpSender.SendMessage(LightPC_IP, LightPC_port, "E");
                //    Thread.Sleep(2000);

                //    UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "F");
                //    UdpSender.SendMessage(LightPC_IP, LightPC_port, "F");
                //    Thread.Sleep(2000);

                //    UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "O");
                //    UdpSender.SendMessage(LightPC_IP, LightPC_port, "O");
                //    Thread.Sleep(2000);

                //    //j += 1;
                //}

                // 建立並保留 callback delegate
                RobotCallback = Test;
            // 1. 開啟連線並註冊 callback
            RobotHandle = HRobot.open_connection("192.168.1.3", 1, RobotCallback);
            if (RobotHandle < 0)
            {
                Console.WriteLine("EntryPoint: 機器人連線失敗，程式終止");
                return;
            }
            Console.WriteLine($"EntryPoint: 連線成功，Handle={RobotHandle}");

            // 2. 設定運行模式與 Override 速度
            HRobot.set_operation_mode(RobotHandle, 1);  // 自動模式
            HRobot.set_override_ratio(RobotHandle, 10); // 覆蓋比例 10%
            Console.WriteLine("EntryPoint: 自動模式啟用，Override Ratio=10");

            // 3. 設定全局 PTP 和直線運動速度
            HRobot.set_ptp_speed(RobotHandle, 50);    // PTP 速度比 50%
            HRobot.set_lin_speed(RobotHandle, 200);   // 直線運動速度 200 mm/s
            Console.WriteLine("EntryPoint: PTP speed=50%, Linear speed=200mm/s");

            const int udpPort = 9999;

            // 4. 模式 A 與 模式 B 交替運行，各持續 15 分鐘
            while (true)
            {
                


                // --- 模式 A ---
                Console.WriteLine("EntryPoint: 切換至 模式A，持續 15 分鐘");
                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "A");
                UdpSender.SendMessage(LightPC_IP, LightPC_port, "A");


                // 在模式 A 開始前，清空任何殘留命令
                HRobot.motion_abort(RobotHandle);
                while (HRobot.get_command_count(RobotHandle) != 0)
                {
                    Thread.Sleep(1);
                }

                DateTime startA = DateTime.Now;
                while (DateTime.Now - startA < TimeSpan.FromMinutes(15))
                {
                    Drill_movement.ExecuteRandom(RobotHandle);
                }
                Console.WriteLine("EntryPoint: 模式A 完成，切換至 模式B");

                // --- 模式 B ---
                Console.WriteLine($"EntryPoint: 切換至 模式B，UDP 監聽 Port={udpPort}，持續 15 分鐘");
                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "B");
                UdpSender.SendMessage(LightPC_IP, LightPC_port, "B");
                // 非阻塞啟動 PoseSequences 監聽
                Task listenerTask = Task.Run(() => PoseSequences.StartListener(udpPort, RobotHandle));
                // 等待 15 分鐘
                Thread.Sleep(TimeSpan.FromMinutes(15));
                Console.WriteLine("EntryPoint: 模式B 時間到，停止監聽並切換");
                // 若需要可在此呼叫 StopListener
                // PoseSequences.StopListener();


                // 確保任務結束再迴圈
                if (!listenerTask.IsCompleted)
                {
                    listenerTask.Wait();
                }
            }
        }

        // Callback 簽章必須與 HRobot.CallBackFun 完全一致
        // 收到動作完成訊號時呼動作完成事件
        private static void Test(ushort cmd, ushort rlt, IntPtr msgPtr, int len)
        {
            PoseSequences.MotionDoneEvent.Set();
        }
    }
}

