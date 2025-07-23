using System;
using System.Runtime.InteropServices;
using System.Threading;
using SDKHrobot;
using IAOAP;

namespace IAOAP
{
    public class EntryPoint
    {
        // 防止 callback delegate 被 GC，需要保留參考
        private static HRobot.CallBackFun RobotCallback;
        // 機器人連線句柄
        private static int RobotHandle;

        public static void Main(string[] args)
        {
            Console.WriteLine("EntryPoint: 開始手臂連線測試");

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
            HRobot.set_override_ratio(RobotHandle, 10); // 覆蓋比例 20%
            Console.WriteLine("EntryPoint: 自動模式啟用，Override Ratio=20");

            // 3. 設定 PTP 和直線運動速度
            HRobot.set_ptp_speed(RobotHandle, 10);    // PTP 速度比 50%
            HRobot.set_lin_speed(RobotHandle, 200);   // 直線運動速度 200 mm/s
            Console.WriteLine("EntryPoint: PTP speed=50%, Linear speed=200mm/s");

            const int udpPort = 9999;

            // 4. 模式 A 與 模式 B 交替運行：各持續15分鐘
            while (true)
            {
                // 模式 A：隨機執行 Drill 動作
                Console.WriteLine("EntryPoint: 切換至 模式A，持續 15 分鐘");
                var startA = DateTime.Now;
                while (DateTime.Now - startA < TimeSpan.FromMinutes(15))
                {
                    Drill_movement.ExecuteRandom(RobotHandle);
                }
                Console.WriteLine("EntryPoint: 模式A 結束，切換至 模式B");

                // 模式 B：UDP 監聽與 PoseSequences
                Console.WriteLine($"EntryPoint: 切換至 模式B，UDP 監聽 Port={udpPort}，持續 15 分鐘");
                PoseSequences.StartListener(udpPort, RobotHandle);
                Console.WriteLine("EntryPoint: 模式B 結束，準備下次切換");
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
