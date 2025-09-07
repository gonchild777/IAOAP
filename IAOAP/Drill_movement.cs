using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SDKHrobot;

namespace IAOAP
{
    /// <summary>
    /// 模式A專用：隨機選擇並執行預先定義的機械手臂動作序列，
    /// 並從 J1List 隨機抽取且不重複的第一軸角度 + 微小偏移量，
    /// 每個姿態可自訂對應的 PTP 速度，並使用動作狀態確認完成。
    /// 第 3 個姿態後開啟 DO8，第 6 個姿態後關閉 DO8。
    /// </summary>
    public static class Drill_movement
    {
        private static readonly Random _rand = new Random(Environment.TickCount);
        private static readonly double[] _offsets = { 0,-1, -2, -3, -4, -5 };

        private static List<(double baseJ1, double offset)> _j1Pairs;
        private static int _nextIndex;

        public static readonly List<double> J1List = Enumerable
            .Range(0, 21)
            .Select(i => -160.0 + i * 15.0)
            .ToList();

        public static readonly Dictionary<string, List<double[]>> Movements = new Dictionary<string, List<double[]>>
        {
            ["DP_01"] = new List<double[]>
            {
                new double[]{ 0.000,    0.000,  0.000,   0.000,  50.000,  0.000 },
                new double[]{ 0.000,   -55.000,  -2.500,   0.000,  54.000,  0.000 },
                new double[]{ 0.000,   -88.000,  34.000,   0.000,  54.000,  0.000 },
                new double[]{ 0.000,   -93.659,  36.387,   0.000,  57.486,  0.000 },
                new double[]{ 0.000,   -95.238,  42.837,   0.000,  52.615,  0.000 },
                new double[]{ 0.000,   -88.000,  34.000,   0.000,  54.000,  0.000 },
                new double[]{ 0.000,   -22.470, -37.850,   0.000,  60.537,  0.000 },
                new double[]{ 0.000,    0.000,   0.000,   0.000,  50.000,  0.000 },
            }
        };

        public static readonly Dictionary<string, List<int>> SpeedSettings = new Dictionary<string, List<int>>
        {
            ["DP_01"] = new List<int> { 50, 50, 30, 5, 2, 10, 20, 50 }
        };

        public static void ExecuteRandom(int handle)
        {
            HRobot.motion_abort(handle);
            while (HRobot.get_command_count(handle) != 0)
                Thread.Sleep(1);

            // 初始化 J1 候選池
            if (_j1Pairs == null || _nextIndex >= _j1Pairs.Count)
            {
                _j1Pairs = J1List
                    .SelectMany(j1 => _offsets.Select(off => (j1, off)))
                    .OrderBy(_ => _rand.Next())
                    .ToList();
                _nextIndex = 0;
                Console.WriteLine($"[Init] J1 洗牌完成，共 {_j1Pairs.Count} 組");
            }

            // 抽取 J1
            var (baseJ1, offset) = _j1Pairs[_nextIndex++];
            double randJ1 = baseJ1 + offset;
            Console.WriteLine($"[Run] 使用第 {_nextIndex}/{_j1Pairs.Count} 組 J1: {baseJ1} + {offset} = {randJ1}");

            // 選擇模式
            var mode = Movements.Keys.ElementAt(_rand.Next(Movements.Count));
            var poses = Movements[mode];
            var speeds = SpeedSettings[mode];

            if (speeds.Count != poses.Count)
            {
                Console.WriteLine($"[Error] 速度設定長度與動作數不符");
                return;
            }

            Console.WriteLine($"[Start] 模式={mode}, 姿態數={poses.Count}");

            for (int i = 0; i < poses.Count; i++)
            {
                var template = poses[i];
                double[] joints = new double[6]
                {
                    randJ1,
                    template[1],
                    template[2],
                    template[3],
                    template[4],
                    template[5]
                };

                HRobot.set_ptp_speed(handle, speeds[i]);

                int ret = HRobot.ptp_axis(handle, 0, joints);
                if (ret != 0)
                {
                    Console.WriteLine($"[Error] ptp_axis 失敗 code={ret}");
                    continue;
                }

                // 等待動作真正完成
                while (HRobot.get_motion_state(handle) != 1)
                {
                    Thread.Sleep(10);
                }

                Console.WriteLine($"[Done] 已完成第 {i + 1}/{poses.Count} 姿態");

                // 控制 DO8：在第3動作（i==2）後開啟，第6動作（i==5）後關閉
                if (i == 2)
                {
                    int r1 = HRobot.set_DO_array(handle, new int[] { 8 }, new int[] { 1 }, 1);
                    Console.WriteLine(r1 == 0 ? "[DO] DO8 已開啟" : $"[Error] 開啟 DO8 失敗 code={r1}");
                }
                if (i == 5)
                {
                    int r2 = HRobot.set_DO_array(handle, new int[] { 8 }, new int[] { 0 }, 1);
                    Console.WriteLine(r2 == 0 ? "[DO] DO8 已關閉" : $"[Error] 關閉 DO8 失敗 code={r2}");
                }
            }

            Console.WriteLine("[End] 全部動作序列執行完成");
        }
    }
}
