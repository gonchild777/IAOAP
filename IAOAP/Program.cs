using System;
using System.Threading;
using System.Threading.Tasks;
using SDKHrobot;
using IAOAP;

namespace IAOAP
{
    public class EntryPoint                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
    {
        public static int ATime =15; //0 slent mode; 15 nomode
        public static string SoundPC_IP = "192.168.1.154";
        public static int SoundPC_port = 8082;
        public static string LightPC_IP = "192.168.1.99";
        public static int LightPC_port = 9897;
        public const int ListenPort = 9999;

        // 防止 callback delegate 被 GC，需要保留參考
        private static HRobot.CallBackFun RobotCallback;
        private static int RobotHandle;

        public static void Main(string[] args)
        {
            Console.WriteLine("EntryPoint: 開始手臂連線測試");

            // 建立並保留 callback delegate
            RobotCallback = Test;
            RobotHandle = HRobot.open_connection("192.168.1.3", 1, RobotCallback);
            if (RobotHandle < 0)
            {
                Console.WriteLine("EntryPoint: 機器人連線失敗，程式終止");
                return;
            }
            Console.WriteLine($"EntryPoint: 連線成功，Handle={RobotHandle}");

            // 設定運行模式
            HRobot.set_operation_mode(RobotHandle, 1);  // 自動模式
            Console.WriteLine("EntryPoint: 自動模式啟用");

            // 設定初始速度（預設給 A）
            HRobot.set_override_ratio(RobotHandle, 10); // 模式 A 用 10%
            HRobot.set_ptp_speed(RobotHandle, 50);
            HRobot.set_lin_speed(RobotHandle, 200);
            Console.WriteLine("EntryPoint: 模式A初始 Override Ratio=20%, PTP=50, LIN=200");

            while (true)
            {
                // --- 模式 A ---
                Console.WriteLine("EntryPoint: 切換至 模式A，持續 15 分鐘");

                // 設定 Override 為模式 A 的值（例如 10%）
                HRobot.set_override_ratio(RobotHandle, 10);
                Console.WriteLine("EntryPoint: 模式A Override Ratio 設定為 10");

                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "A");
                UdpSender.SendMessage(LightPC_IP, LightPC_port, "A");

                // 清空殘留命令
                HRobot.motion_abort(RobotHandle);
                while (HRobot.get_command_count(RobotHandle) != 0)
                    Thread.Sleep(1);

                DateTime startA = DateTime.Now;
                while (DateTime.Now - startA < TimeSpan.FromMinutes(ATime))
                {
                    Drill_movement.ExecuteRandom(RobotHandle);
                }
                Console.WriteLine("EntryPoint: 模式A 完成，切換至 模式B");

                // --- 模式 B ---
                Console.WriteLine($"EntryPoint: 切換至 模式B，UDP 監聽 Port={ListenPort}，持續 15 分鐘");

                // 設定 Override 為模式 B 的值（例如 20%）
                HRobot.set_override_ratio(RobotHandle, 30);
                Console.WriteLine("EntryPoint: 模式B Override Ratio 設定為 20");

                UdpSender.SendMessage(SoundPC_IP, SoundPC_port, "B");
                UdpSender.SendMessage(LightPC_IP, LightPC_port, "B");

                Task listenerTask = Task.Run(() => PoseSequences.StartListener(ListenPort, RobotHandle));
                Thread.Sleep(TimeSpan.FromMinutes(15));

                Console.WriteLine("EntryPoint: 模式B 時間到，停止監聽並切換");

                if (!listenerTask.IsCompleted)
                    listenerTask.Wait();
            }
        }

        private static void Test(ushort cmd, ushort rlt, IntPtr msgPtr, int len)
        {
            PoseSequences.MotionDoneEvent.Set();
        }
    }
}
