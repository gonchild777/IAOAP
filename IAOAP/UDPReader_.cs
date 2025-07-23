using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IAOAP
{
    // 對應 JSON 結構的類別
    public class Position
    {
        public int x1 { get; set; }
        public int y1 { get; set; }
        public int x2 { get; set; }
        public int y2 { get; set; }
    }

    public class ZoneData
    {
        public int PeopleCount { get; set; }
        public List<Position> PeoplePosition { get; set; }
    }

    public class Region
    {
        public string ID { get; set; }
        public List<ZoneData> Zone { get; set; }
    }

    public static class UDPProgram
    {
        // 監聽 UDP 的埠號，可自行調整
        const int ListenPort = 9999;

        // ID 到區域字母的對應
        static readonly Dictionary<string, string> IdToZone = new Dictionary<string, string>
        {
            ["1"] = "A",
            ["2"] = "B",
            ["3"] = "C",
            ["4"] = "D"
        };

        public static async Task Main(string[] args)
        {
            // 传统 using 块，确保 udp 在整个块中可见
            using (var udp = new UdpClient(ListenPort))
            {
                Console.WriteLine($"開始監聽 UDP（埠號 {ListenPort}）...");

                while (true)
                {
                    // 非同步接收
                    var result = await udp.ReceiveAsync();
                    var jsonText = Encoding.UTF8.GetString(result.Buffer);

                    try
                    {
                        var regions = JsonConvert.DeserializeObject<List<Region>>(jsonText);
                        PrintFilenames(regions);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"解析 JSON 失敗：{ex.Message}");
                    }
                }
            } // using 结尾，udp.Dispose() 会在这里被调用
        }


        static void PrintFilenames(List<Region> regions)
        {
            foreach (var reg in regions)
            {
                // 只處理 ID 1~4
                if (!IdToZone.TryGetValue(reg.ID, out var zone))
                    continue;

                var zoneData = reg.Zone?[0];
                if (zoneData == null)
                    continue;

                int count = zoneData.PeopleCount;
                string filename;

                if (count >= 1 && count <= 3)
                {
                    filename = $"joints_data_{count}-{zone}.json";
                }
                else if (count > 3)
                {
                    filename = "joints_data_more_than_3.json";
                }
                else
                {
                    // count == 0，跳過
                    continue;
                }

                Console.WriteLine($"區域 {zone} 偵測到 {count} 人 → 檔名：{filename}");
            }
        }
    }
}
