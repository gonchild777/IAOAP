using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SDKHrobot;
using IAOAP;


namespace IAOAP
{
    /// <summary>
    /// 模式A專用：隨機選擇並執行預先定義的機械手臂動作序列，
    /// 並從 J1List 隨機抽取且不重複的第一軸角度 + 微小偏移量，
    /// 每個姿態可自訂對應的 PTP 速度，並使用事件驅動等待。
    /// 第 3 個姿態後開啟 DO8，第 6 個姿態後關閉 DO8。
    /// </summary>
    public static class Drill_movement
    {
        private static readonly Random _rand = new Random();
        private static readonly double[] _offsets = { 0, 0.35, 0.7, -0.35, -0.7 };

        // 使用 double 作為 offset 類型
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
                new double[]{   0.000,    0.000,  0.000,   0.000,  50.000,  0.000 },
                new double[]{   0.000,   -55.000,  -2.500,   0.000,  54.000,  0.000 },
                new double[]{   0.000,   -88.000,  34.000,   0.000,  54.000,  0.000 },
                new double[]{   0.000,   -93.659,  36.387,   0.000,  57.486,  0.000 },
                new double[]{   0.000,   -95.919,  42.343,   0.000,  53.790,  0.000 },
                new double[]{   0.000,   -86.145,  32.165,   0.000,  54.193,  0.000 },
                new double[]{   0.000,   -78.669,  29.800,   0.000,  49.083,  0.000 },
                new double[]{   0.000,   -22.470, -37.850,   0.000,  60.537,  0.000 },
                new double[]{   0.000,    0.000,   0.000,   0.000,  50.000,  0.000 },
            }
        };

        public static readonly Dictionary<string, List<int>> SpeedSettings = new Dictionary<string, List<int>>
        {
            ["DP_01"] = new List<int> { 50, 50, 30, 5, 2,2, 10, 50, 50 }
        };

        public static void ExecuteRandom(int handle)
        {
            // 清空殘留命令
            HRobot.motion_abort(handle);
            while (HRobot.get_command_count(handle) != 0)
                Thread.Sleep(1);

            // 初始化或重置 J1 候選池（正確使用 double）
            if (_j1Pairs == null || _nextIndex >= _j1Pairs.Count)
            {
                _j1Pairs = J1List
                    .SelectMany(j1 => _offsets.Select(off => (j1, off)))
                    .OrderBy(_ => _rand.Next())
                    .ToList();
                _nextIndex = 0;
                Console.WriteLine($"[Drill_movement] 新一輪 J1 洗牌完成，共 {_j1Pairs.Count} 組");
            }

            // 取出下一組 J1 + offset（不重複）
            var (baseJ1, offset) = _j1Pairs[_nextIndex++];
            double randJ1 = baseJ1 + offset;

            Console.WriteLine($"[Drill_movement] 使用第 {_nextIndex}/{_j1Pairs.Count} 組 J1: {baseJ1} + {offset} = {randJ1}");

            // 選模式與資料
            var mode = Movements.Keys.ElementAt(_rand.Next(Movements.Count));
            var poses = Movements[mode];

            if (!SpeedSettings.TryGetValue(mode, out var speeds) || speeds.Count != poses.Count)
            {
                Console.WriteLine($"Drill_movement: 模式 {mode} 無速度設定或長度不符");
                return;
            }

            Console.WriteLine($"Drill_movement: 執行 模式={mode}, 姿態數={poses.Count}");

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

                PoseSequences.MotionDoneEvent.Reset();
                int ret = HRobot.ptp_axis(handle, 0, joints);
                if (ret != 0)
                {
                    Console.WriteLine($"Drill_movement: ptp_axis 失敗 code={ret}");
                    continue;
                }

                PoseSequences.MotionDoneEvent.WaitOne();
                Console.WriteLine($"Drill_movement: 完成 {i + 1}/{poses.Count}");

                // 特定步驟打開/關閉 DO8
                if (i == 2)
                {
                    int[] idxOn = { 8 };
                    int[] valOn = { 1 };
                    int r1 = HRobot.set_DO_array(handle, idxOn, valOn, 1);
                    Console.WriteLine(r1 == 0 ? "Drill_movement: DO8 已開啟" : $"Drill_movement: 開啟 DO8 失敗 code={r1}");
                }
                if (i == 5)
                {
                    int[] idxOff = { 8 };
                    int[] valOff = { 0 };
                    int r2 = HRobot.set_DO_array(handle, idxOff, valOff, 1);
                    Console.WriteLine(r2 == 0 ? "Drill_movement: DO8 已關閉" : $"Drill_movement: 關閉 DO8 失敗 code={r2}");
                }
            }

            Console.WriteLine("Drill_movement: 全部動作序列執行完成");
        }
    }
}
