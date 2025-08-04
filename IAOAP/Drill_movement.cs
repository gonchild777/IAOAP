using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SDKHrobot;

namespace IAOAP
{
    /// <summary>
    /// 模式A專用：隨機選擇並執行預先定義的機械手臂動作序列，
    /// 並從 J1List 隨機抽取且不重複的第一軸角度，
    /// 每個姿態可自訂對應的 PTP 速度，並在第4與第6動作時控制 DO8
    /// </summary>
    public static class Drill_movement
    {
        private static readonly Random _rand = new Random();

        // 定義 J1 偏移量
        private static readonly int[] _offsets = { 0, 1, 2, -1, -2 };

        // J1 組合池與讀取索引
        private static List<(double baseJ1, int offset)> _j1Pairs;
        private static int _nextIndex;

        /// <summary>
        /// J1 值範圍：-150 到 +150，步距 15，共 21 個
        /// </summary>
        public static readonly List<double> J1List = Enumerable
            .Range(0, 21)
            .Select(i => -150.0 + i * 15.0)
            .ToList();

        /// <summary>
        /// 動作序列範例：Key=模式名稱，Value=姿態列表 (J1~J6)
        /// </summary>
        public static readonly Dictionary<string, List<double[]>> Movements = new Dictionary<string, List<double[]>>
        {
            ["DP_01"] = new List<double[]>
            {
                new double[] { 0.000,  0.000,   0.0000, 0.000, 50.000, 0.000 },
                new double[] { 0.000, -55.000,  -2.5000, 0.000, 54.000, 0.000 },
                new double[] { 0.000, -88.000,  34.000, 0.000, 54.000, 0.000 },
                new double[] { 0.000, -91.083,  36.942, 0.000, 54.356, 0.000 },
                new double[] { 0.000, -99.897,  57.503, 0.000, 42.609, 0.000 },
                new double[] { 0.000, -91.083,  36.942, 0.000, 54.356, 0.000 },
                new double[] { 0.000, -86.145,  32.165, 0.000, 54.193, 0.000 },
                new double[] { 0.000, -78.669,  29.800, 0.000, 49.083, 0.000 },
                new double[] { 0.000, -22.470, -37.850, 0.000, 60.537, 0.000 },
                new double[] { 0.000,   0.000,   0.0000, 0.000, 50.000, 0.000 }
            }
        };

        /// <summary>
        /// 對應每個姿態的 PTP 速度百分比
        /// </summary>
        public static readonly Dictionary<string, List<int>> SpeedSettings = new Dictionary<string, List<int>>
        {
            ["DP_01"] = new List<int> { 50, 50, 30, 10, 10, 10, 10, 30, 50, 50 }
        };

        /// <summary>
        /// 執行隨機動作序列
        /// </summary>
        public static void ExecuteRandom(int handle)
        {
            // 設定 DO8 為數位輸出並保存
            HRobot.set_SO_array(handle, new int[] { 8 }, new int[] { 0 }, 1);
            HRobot.save_module_io_setting(handle);

            // 初始化或重置 J1 候選
            if (_j1Pairs == null || _nextIndex >= _j1Pairs.Count)
            {
                _j1Pairs = J1List
                    .SelectMany(j1 => _offsets.Select(off => (j1, off)))
                    .OrderBy(_ => _rand.Next())
                    .ToList();
                _nextIndex = 0;
            }

            // 取出隨機 J1
            var (baseJ1, offset) = _j1Pairs[_nextIndex++];
            double randJ1 = baseJ1 + offset;

            // 隨機選模式
            var mode = Movements.Keys.ElementAt(_rand.Next(Movements.Count));
            var poses = Movements[mode];

            // 取得速度設定
            if (!SpeedSettings.TryGetValue(mode, out var speeds) || speeds.Count != poses.Count)
            {
                Console.WriteLine($"Drill_movement: 模式 {mode} 無速度設定或長度不符");
                return;
            }

            Console.WriteLine($"Drill_movement: 執行 模式={mode}, J1={randJ1}, 共 {poses.Count} 姿態");

            for (int i = 0; i < poses.Count; i++)
            {
                // 在第4步前開 DO8
                if (i == 3)
                {
                    HRobot.set_SO_array(handle, new int[] { 8 }, new int[] { 1 }, 1);
                    Console.WriteLine("Drill_movement: DO8 已開啟");
                }
                // 在第6步後關 DO8
                if (i == 5)
                {
                    HRobot.set_SO_array(handle, new int[] { 8 }, new int[] { 0 }, 1);
                    Console.WriteLine("Drill_movement: DO8 已關閉");
                }

                var template = poses[i];
                double[] joints = new double[6]
                {
                    randJ1,
                    template[1], template[2], template[3], template[4], template[5]
                };

                // 設定 PTP 速度並啟動動作
                HRobot.set_ptp_speed(handle, speeds[i]);
                PoseSequences.MotionDoneEvent.Reset();
                int ret = HRobot.ptp_axis(handle, 0, joints);
                if (ret != 0)
                {
                    Console.WriteLine($"Drill_movement: ptp_axis 失敗 code={ret}");
                    continue;
                }

                // 等待完成
                PoseSequences.MotionDoneEvent.WaitOne();
                Console.WriteLine($"Drill_movement: 完成 {i + 1}/{poses.Count}");
            }

            Console.WriteLine("Drill_movement: 全部動作序列執行完成");
        }
    }
}
