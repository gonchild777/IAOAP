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
    /// 每個姿態可自訂對應的 PTP 速度
    /// </summary>
    public static class Drill_movement
    {
        private static readonly Random _rand = new Random();

        // 偏移量，基準J1加上這些偏移
        private static readonly int[] _offsets = { 0, 1, 2, -1, -2 };

        // 用來儲存所有23*5=115種基準+偏移候選，並追蹤索引
        private static List<(double baseJ1, int offset)> _j1Pairs;
        private static int _nextIndex;

        /// <summary>
        /// J1 可能值：從 -165 到 +165，間隔 15，共 23 個
        /// </summary>
        public static readonly List<double> J1List = Enumerable
            .Range(0, 23)
            .Select(i => -165.0 + 15.0 * i)
            .ToList();

        /// <summary>
        /// 動作序列集合：Key 為模式名稱，Value 為多組關節角度 (J1~J6)，J1 位置預設為 0
        /// </summary>
        public static readonly Dictionary<string, List<double[]>> Movements = new Dictionary<string, List<double[]>>
        {
            ["DP_01"] = new List<double[]>
            {
            // 起點4
            new double[]{   0.000,    0.000,  0.000,   0.000,  50.000,  0.000 },
            //低下頭0
            new double[]{   0.000,    -55.000,  -2.5000,   0.000,  54.000,  0.000 },


            //下去溝槽1(中間)
            new double[]{   0.000,    -88.000,  34.000,   0.000,   54.000,  0.000 },
            //開始鑽7(中間往下)
            new double[]{   0.000,    -91.083,  36.942,   0.000,   54.356,  0.000 },
            //往前走11
            new double[]{   0.000,    -99.897,  57.503,   0.000,   42.609,  0.000 },
            //回到7
            new double[]{   0.000,    -91.083,  36.942,   0.000,   54.356,  0.000 },
            //溝槽起來
            new double[]{   0.000,    -86.145,  32.165,   0.000,   54.193,  0.000 },
            //抬起來3
            new double[]{   0.000,    -78.669,  29.800,   0.000,   49.083,  0.000 },
            //回到低頭5
            new double[]{   0.000,    -22.470,  -37.850,   0.000,   60.537,  0.000 },
            //回原點6
            new double[]{   0.000,    0.000,  0.000,   0.000,  50.000,  0.000 }
            }
            // TODO: 新增更多動作模式
        };

        /// <summary>
        /// 每個動作模式對應的 PTP 速度清單，與 Movements 中的姿態數量必須一致
        /// </summary>
        public static readonly Dictionary<string, List<int>> SpeedSettings = new Dictionary<string, List<int>>
        {
            // DP_01 有兩筆姿態，對應兩個速度值
            ["DP_01"] = new List<int> { 50,50,30,10,10,10,10, 30,50,50 }
            // TODO: 為其他模式增加速度列表
        };

        /// <summary>
        /// 隨機選一個動作序列並執行：
        /// 1. 從不重複清單中取 baseJ1+offset
        /// 2. 隨機選動作模式
        /// 3. 對每個姿態，使用對應的速度設定並執行 ptp_axis
        /// </summary>
        public static void ExecuteRandom(int handle)
        {
            // 初始化或耗盡候選後，重置並隨機洗牌
            if (_j1Pairs == null || _nextIndex >= _j1Pairs.Count)
            {
                _j1Pairs = J1List
                    .SelectMany(baseJ1 => _offsets.Select(off => (baseJ1, off)))
                    .OrderBy(_ => _rand.Next())
                    .ToList();
                _nextIndex = 0;
                Console.WriteLine($"Drill_movement: 重置 J1 候選，共 {_j1Pairs.Count} 種");
            }

            // 取出下一個基準+偏移，並計算實際 J1
            var pair = _j1Pairs[_nextIndex++];
            double randJ1 = pair.baseJ1 + pair.offset;
            Console.WriteLine($"Drill_movement: 基準J1={pair.baseJ1}, 偏移={pair.offset}, 使用J1={randJ1}");

            // 隨機選一個動作模式
            var keys = Movements.Keys.ToList();
            string key = keys[_rand.Next(keys.Count)];
            var seqList = Movements[key];
            Console.WriteLine($"Drill_movement: 執行模式 [{key}]，共 {seqList.Count} 組姿態");

            // 取得對應速度列表
            if (!SpeedSettings.TryGetValue(key, out var speeds) || speeds.Count != seqList.Count)
            {
                Console.WriteLine($"Drill_movement: 找不到或長度不符的速度設定，模式={key}");
                

            // 清空先前未執行完的命令，避免隊列堆積

            }

            // 執行每筆姿態
            for (int i = 0; i < seqList.Count; i++)
            {
                var template = seqList[i];
                double[] joints = new double[6];
                joints[0] = randJ1;
                for (int j = 1; j < 6; j++)
                    joints[j] = template[j];

                // 取速度
                int stepSpeed = speeds[i];
                Console.WriteLine($"Drill_movement: 第{i + 1}筆姿態，PTP速度={stepSpeed}%");
                int spRet = HRobot.set_ptp_speed(handle, stepSpeed);
                if (spRet != 0)
                    Console.WriteLine($"Drill_movement: set_ptp_speed 失敗，錯誤碼：{spRet}");

                // 清除上次動作完成訊號

                // 清空先前未執行完的命令，避免隊列堆積
                HRobot.motion_abort(handle);
                while (HRobot.get_command_count(handle) != 0)
                    Thread.Sleep(0);

                // 呼叫平滑 PTP (mode=1)
                string jointStr = string.Join(",", joints.Select(j => j.ToString()));
                Console.WriteLine($"Drill_movement: ptp_axis, joints=[{jointStr}]");
                int ret = HRobot.ptp_axis(handle, 1, joints);

                if (ret != 0)
                {
                    Console.WriteLine($"Drill_movement: ptp_axis 失敗，錯誤碼：{ret}");
                    continue;
                }

                // 改為 polling 等待動作完成，避免事件排隊造成延遲
                while (HRobot.get_motion_state(handle) != 0)
                    Thread.Sleep(0); // 更改為 Sleep(0) 以降低輪詢延遲

                Console.WriteLine("Drill_movement: 動作完成"); // 完成
                //Thread.Sleep(500); // 已移除固定延遲，以減少動作間停頓
            }
        }
    }
}
