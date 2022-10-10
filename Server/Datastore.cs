using Newtonsoft.Json;
using Opc.Ua;

namespace BeltLightSensor;

public class Datastore
{
    public Datastore(string fileName = "datastore.db")
    {
        var folder = Environment.CurrentDirectory;
        DbPath = Path.Combine(folder, fileName);

        Records = new List<BeltLightResults>();

        if (File.Exists(DbPath))
            if (!Load())
                throw new Exception("Failed to load datastore");

        Console.WriteLine($"Datastore path: {DbPath}");
    }

    public List<BeltLightResults>? Records { get; private set; }

    public string DbPath { get; }

    public bool Save()
    {
        Console.WriteLine("Datastore: Saving");

        var json = JsonConvert.SerializeObject(Records);
        try
        {
            File.WriteAllText(DbPath, json);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error saving datastore: {e.Message}");
            return false;
        }

        return true;
    }

    public bool Load()
    {
        Console.WriteLine("Datastore: Loading");

        if (!File.Exists(DbPath)) return false;

        var json = File.ReadAllText(DbPath);
        Records = JsonConvert.DeserializeObject<List<BeltLightResults>>(json);

        return true;
    }

    public void AddRecord(BeltLightResults record)
    {
        Records?.Add(record);
    }

    public void PrintRecords()
    {
        foreach (var record in Records ?? new List<BeltLightResults>())
            Console.WriteLine($"Time: {record.Timestamp} " +
                              $"Channels count: {record.Values.Length} ");
    }

    public IEnumerable<BeltLightResults> GetRecordsBetweenTimestamps(DateTime start, DateTime end)
    {
        return Records?
            .Where(r => r.Timestamp >= start && r.Timestamp <= end)
            .ToList() ?? new List<BeltLightResults>();
    }

    public IEnumerable<BeltLightResults> GetRecordsBetweenTimestamps(DateTime start, DateTime end, int offset,
        int limit)
    {
        return Records?
            .Where(r => r.Timestamp >= start && r.Timestamp <= end)
            .Skip(offset)
            .Take(limit)
            .ToList() ?? new List<BeltLightResults>();
    }
}

public record BeltLightResults(float[][] Values)
{
    public DateTime Timestamp { get; } = DateTime.Now;
}

public static class CustomExtensions
{
    public static HistoryData AsHistoryData(this IEnumerable<BeltLightResults> records)
    {
        var data = new HistoryData();

        foreach (var item in records)
            data.DataValues.Add(new DataValue(
                new Variant(item.Values),
                StatusCodes.Good,
                item.Timestamp,
                item.Timestamp));

        return data;
    }

    public static BeltLightResults? AsBeltLightResults(this WriteValue item)
    {
        return item.Value.Value is float[][] values ? new BeltLightResults(values) : null;
    }

    public static BeltLightResults? FromStringToArray(this WriteValue item)
    {
        var str = item.Value.Value is string values ? values : null;
        if (str == null) return null;

        str = str.TrimEnd(';');

        // var strExample =
        //     "6,25,121,76,90,91,270,150,70,26,5,2,90,26,12,8,16,10;6,25,124,78,95,96,336,151,70,26,5,2,98,28,12,8,19,12;6,25,122,81,101,98,455,173,77,28,5,2,104,30,12,8,24,16;7,26,123,85,109,102,540,197,79,29,5,2,115,32,13,9,31,19;7,27,131,92,115,104,722,240,86,30,6,3,127,35,14,10,40,25;7,30,148,118,156,134,880,298,91,32,6,3,141,37,14,10,52,32;9,36,171,141,178,155,1073,377,97,34,7,3,170,44,16,12,74,51;11,62,277,196,264,200,1535,491,107,38,9,3,198,50,18,14,98,72;11,54,245,176,272,179,2574,671,122,44,12,3,231,57,19,15,118,76;9,39,172,186,326,176,4018,976,148,54,16,4,263,60,19,15,124,72;11,41,171,226,406,192,3824,1198,165,60,18,4,289,62,19,15,119,66;11,42,172,272,494,212,3144,1253,174,61,16,4,303,64,19,15,92,52;12,46,180,343,604,242,2506,1077,175,56,14,4,290,64,18,14,62,37;13,49,186,422,705,278,1354,513,171,51,9,4,233,57,17,12,36,26;12,52,191,495,830,327,1150,559,164,55,12,4,316,73,23,16,50,30;12,56,200,604,1005,403,1060,651,227,61,14,6,347,78,25,18,54,29;13,74,232,736,1169,539,1088,860,355,71,17,7,514,112,31,24,67,31;15,96,280,899,1517,677,1221,1103,468,86,21,8,676,145,35,30,86,37;17,107,323,1165,2068,848,1411,1346,499,93,22,8,831,175,36,35,126,51;20,146,421,1618,2909,1179,1648,1600,426,93,19,8,872,181,35,36,201,84;26,229,580,1831,3069,1497,2407,2002,354,98,18,9,812,165,36,35,288,136;31,325,738,1836,2818,1720,4475,2454,301,94,17,9,540,119,35,27,182,110;35,396,883,1689,2447,1728,5212,1843,271,82,20,9,339,86,29,18,115,64;42,475,1086,1347,1984,1626,1668,647,190,55,14,7,194,55,20,14,67,38;48,563,1362,1175,1741,1656,1135,484,140,45,10,4,169,48,16,11,48,26;34,350,841,512,816,565,761,400,129,42,9,3,161,46,15,10,34,19;71,834,2239,1534,2304,2181,598,347,122,40,8,3,149,43,14,9,30,17;79,945,2357,1717,2617,2530,471,325,115,39,7,3,143,41,13,9,26,14;88,1082,2608,1967,2997,2829,465,250,114,40,8,3,142,42,14,9,26,15;80,1057,2160,2280,3619,3426,581,346,127,44,9,3,167,49,17,11,31,18;66,870,1530,2953,5014,4237,603,353,130,43,9,3,164,48,15,10,30,18;51,664,1052,4990,7720,5407,633,347,123,42,9,3,168,48,15,11,31,20;44,485,859,5268,8206,4737,872,542,202,53,12,3,292,77,19,16,53,31;36,370,811,2642,4195,2638,1289,864,296,68,15,4,504,125,25,24,121,59;30,286,724,2282,3407,2139,2464,1788,413,104,20,6,843,200,34,38,161,81;21,184,423,1194,1719,1171,3446,2428,587,146,28,7,1260,293,47,55,196,95;21,210,432,882,1269,1021,3773,2918,898,182,39,9,1661,378,65,72,234,111;23,265,506,783,1175,1035,4458,3509,1302,218,53,13,1670,375,78,72,228,114;25,320,578,815,1237,1127,4760,3693,1514,232,62,18,1280,291,73,55,181,96;25,267,641,819,1217,1171,4370,3213,1412,207,60,22,1190,279,77,53,172,92;35,468,876,985,1472,1382,3747,2446,1021,161,48,21,1165,271,79,53,173,92;38,536,1002,1143,1710,1617,3222,1925,674,123,37,18,1026,239,69,48,163,90;44,597,1163,1257,1923,1803,1939,1212,427,90,24,12,652,156,46,33,80,42;51,670,1389,1478,2425,2168,2450,1018,254,75,18,7,207,60,21,15,108,71;61,776,1713,2708,4534,3613,2420,988,204,70,15,6,284,75,28,19,126,73;81,982,2177,4685,7175,6197,2932,1407,199,85,15,6,360,91,24,20,162,84;110,1547,2841,4550,7127,6521,3405,2033,221,128,18,6,753,146,25,31,221,98;165,2500,4561,5394,8036,7516,3443,3164,554,201,34,7,1846,265,56,59,252,99;171,2334,4813,4945,7186,6612,3110,3312,1552,226,59,29,2109,346,127,82,215,83;121,1543,3884,3099,4859,4366,2435,2993,1629,185,60,47,1833,263,130,80,175,42;106,1346,3665,2505,3956,3373,637,2366,1546,62,52,49,1001,72,122,66,65,12;105,1349,3747,2599,4090,3502,233,802,1204,30,25,43,474,35,82,30,32,4;100,1319,3734,2430,3766,3048,49,372,565,14,11,26,75,16,53,16,7,3;89,1192,3378,2073,2986,2443,51,67,132,13,3,11,69,19,27,13,8,4;54,653,1962,1544,2544,1703,68,89,79,17,3,8,80,23,30,16,9,4;38,430,1247,1209,1998,1482,65,96,90,19,4,9,61,17,21,12,7,3;70,850,2519,1505,2649,1754,51,73,68,14,3,5,57,15,20,12,6,3;78,980,2768,1864,3134,2141,39,56,49,11,2,6,67,19,22,14,8,4;98,1296,3445,2443,3521,2569,53,78,69,15,3,6,67,19,20,14,8,4;132,1563,4159,2065,3038,2362,53,78,69,15,3,6,68,19,18,14,8,4;116,1335,3506,1572,2163,1694,52,76,71,15,3,5,68,19,18,13,7,3;85,918,2229,786,1084,480,49,71,63,14,3,5,70,19,18,13,7,3;50,468,779,339,386,214,48,72,64,14,3,4,71,19,17,13,8,3;30,237,398,63,83,78,53,75,66,15,3,4,70,20,18,13,8,4;10,60,142,59,61,68,60,81,71,17,3,4,74,21,18,13,8,3;10,73,192,80,81,85,55,81,75,17,3,4,60,17,16,11,6,3;9,64,177,62,64,71,51,75,68,14,3,4,52,14,11,8,5,2;7,34,99,42,45,51,38,57,52,11,2,3,54,15,13,11,6,3;6,42,90,37,43,47,38,55,48,11,2,3,59,18,13,11,6,3;7,45,112,48,52,60,46,66,56,13,3,3,60,17,13,10,6,3;7,44,114,48,52,60,45,64,55,13,3,3,56,16,12,9,6,3;7,44,114,48,52,60,45,63,54,13,2,3,62,24,19,17,8,4;7,43,114,48,52,61,38,55,49,11,2,3,60,22,13,11,7,3;7,43,117,49,52,61,47,67,58,13,3,3,54,17,11,9,6,3;7,42,116,50,53,61,56,72,59,16,3,3,64,18,13,9,7,3;7,48,144,63,64,67,55,72,61,15,3,3,62,18,14,10,6,3;7,50,148,57,58,62,50,71,62,14,3,3,47,13,10,6,5,2;6,27,91,40,41,47,41,58,50,11,2,2,50,13,10,8,5,2;6,32,86,39,41,47,44,60,51,12,2,2,56,16,11,8,6,3;6,32,88,40,41,48,40,55,47,11,2,2,56,15,11,8,6,3;7,33,99,44,46,54,46,66,57,13,3,3,57,16,12,9,6,3;7,34,96,43,46,53,49,70,62,13,3,3,60,16,12,9,6,3;7,35,96,43,47,54,49,68,58,13,3,3,73,18,14,10,7,3;7,32,98,42,42,53,53,71,59,15,2,3,68,23,16,17,8,4;8,49,97,49,49,55,54,70,61,17,3,3,74,23,16,12,7,3;8,45,133,65,64,68,45,69,65,14,3,3,58,15,12,9,6,3;8,48,158,61,61,67,38,57,52,12,2,2,64,18,12,10,6,3;7,30,107,46,43,51,39,57,52,11,2,2,66,18,13,11,6,3;6,28,88,37,40,48,36,52,44,10,2,2,61,19,12,11,6,3;6,30,88,38,39,46,39,56,50,12,2,3,73,19,16,11,8,4;7,32,93,43,43,51,41,60,51,11,2,2,68,19,15,11,7,3;7,35,106,44,48,59,40,57,49,11,2,2,73,20,14,11,7,3;7,35,113,48,49,57,46,60,50,13,2,2,74,22,14,11,8,4;7,33,104,46,47,56,51,61,52,13,2,3,80,20,17,11,7,3;7,39,123,67,67,72,41,59,56,11,2,3,62,16,11,7,6,3;9,46,156,63,62,70,34,47,42,10,2,2,65,16,12,10,6,3;8,42,128,49,52,62,39,53,47,10,2,2,63,18,12,9,6,3;6,32,93,40,43,53,36,49,44,10,2,2,68,18,13,11,7,3;8,37,105,42,46,58,38,54,47,10,2,2,67,18,13,10,7,3;7,38,99,43,46,57,41,55,48,11,2,2,67,18,13,10,7,3;7,31,96,42,44,56,44,57,49,11,2,2,73,20,13,10,7,3;7,32,95,38,42,52,57,62,52,14,2,2,76,22,15,12,7,4;7,31,90,45,44,50,47,57,51,12,2,2,64,18,14,10,6,3;7,39,130,62,64,71,39,49,46,10,2,2,61,16,11,9,6,3;8,47,157,59,63,72,39,53,49,10,2,2,63,17,12,11,7,3;6,28,94,39,40,48,38,47,44,10,2,2,66,19,13,11,7,4;6,31,87,36,39,48,45,51,44,10,2,2,69,19,13,11,7,4;6,33,90,38,41,50,37,49,46,10,2,2,68,19,13,11,7,4;7,33,94,39,42,52,41,45,42,9,2,2,69,19,13,11,7,4;6,34,96,39,44,54,43,48,43,11,2,2,74,22,14,11,8,4;7,34,101,42,46,56,42,45,42,11,2,2,73,22,15,12,7,4;7,34,101,45,47,57,57,84,84,12,3,11,57,16,12,8,7,4;7,42,147,61,63,70,44,51,49,9,2,2,61,17,11,9,7,4;7,44,150,49,53,65,47,51,43,9,2,2,60,17,12,10,6,3;6,25,87,37,41,50,48,51,45,9,2,2,60,15,11,9,7,3;6,32,91,36,41,50,43,39,39,9,2,2,69,16,14,8,7,3;6,31,92,36,41,51,38,46,47,8,2,2,69,18,12,10,7,3;7,35,100,42,50,61,43,44,40,9,2,2,66,21,16,18,8,4;7,34,105,42,47,59,58,50,43,11,2,2,60,20,13,10,7,4;7,36,109,52,59,66,112,48,38,10,2,2,49,14,10,8,6,3;7,35,111,49,51,59,44,37,35,8,1,2,43,13,8,6,5,3;7,42,135,51,55,62,50,49,50,8,2,2,44,14,9,8,6,3;6,30,97,35,40,46,54,49,44,10,2,2,58,16,12,10,7,4;8,48,169,56,68,83,53,47,44,9,2,2,64,18,12,10,7,4;7,47,129,50,58,71,42,38,34,9,2,2,59,18,12,10,7,3;5,29,79,33,42,50,55,49,37,11,2,2,69,22,14,11,8,4;5,29,88,35,45,56,64,56,40,12,2,2,56,17,12,10,7,4;5,28,79,35,44,52,62,51,36,10,2,2,55,17,10,8,7,3;5,31,92,45,50,56,62,52,37,10,2,2,59,18,12,10,8,4;6,40,128,53,62,68,55,44,33,10,2,2,59,18,12,10,7,4;5,28,87,35,45,52,66,58,38,11,2,2,73,22,14,11,8,4;4,25,74,35,47,54,73,63,38,13,2,2,63,19,13,10,8,4;5,28,80,38,50,58,72,58,36,12,2,2,59,17,10,7,7,4;5,27,77,37,50,58,74,58,33,12,2,2,66,19,12,10,8,4;5,28,82,40,54,63,69,63,37,12,2,2,66,20,12,11,8,4;5,28,84,40,46,55,63,63,39,13,2,2,70,24,13,11,8,4;5,37,121,55,70,75,90,78,43,15,3,2,68,22,13,11,8,4;5,33,110,44,59,67,93,66,37,14,2,2,60,19,9,7,7,4;4,23,80,36,61,72,81,68,35,14,2,2,64,18,11,9,8,4;5,29,84,43,58,66,83,71,35,14,2,2,67,18,11,9,8,4;5,30,92,47,61,71,68,55,35,13,2,2,60,17,12,10,7,4;5,35,115,51,66,72,85,78,43,15,2,2,76,23,13,11,9,4;6,37,116,52,68,76,87,82,43,15,3,2,58,20,10,8,7,3;4,23,83,46,66,73,85,76,35,14,2,2,59,19,11,11,7,4;5,32,87,47,69,75,87,80,38,14,3,2,62,21,10,10,8,4;5,34,98,50,72,82,71,61,37,14,2,2,53,15,10,7,7,3;5,38,120,51,68,76,95,95,48,16,3,2,68,19,12,8,8,4;6,42,136,60,82,91,94,93,45,16,3,2,64,18,9,8,8,4;4,25,90,47,69,80,93,91,40,15,3,2,72,20,11,10,9,4;5,34,98,50,72,82,76,78,36,14,2,2,64,20,13,11,8,4;5,36,102,52,79,93,95,93,50,17,3,2,83,25,15,12,10,5;5,35,107,52,68,78,92,100,50,18,3,2,72,20,10,8,9,4;6,43,137,66,84,93,114,105,44,17,3,2,86,22,12,11,11,4;5,36,117,54,78,88,102,107,50,17,3,2,77,22,12,11,9,4;5,37,114,56,81,92,71,75,41,15,3,2,82,24,14,12,9,4;5,36,113,49,72,90,104,116,56,18,3,2,80,22,12,10,9,4;5,32,106,50,69,83,102,109,51,17,3,2,81,22,10,8,10,4;6,45,146,71,91,101,105,116,54,17,3,2,87,23,13,13,10,5;5,34,107,57,82,93,106,114,57,18,3,3,73,20,11,9,9,4;4,36,108,61,86,95,80,94,58,17,3,3,89,24,12,10,9,4;5,38,116,58,73,81,114,132,69,19,4,3,87,23,11,8,10,4;6,50,170,76,98,108,111,123,60,18,3,2,84,22,10,8,10,4;6,46,143,72,95,100,115,130,66,19,4,3,86,23,11,9,10,4;5,41,135,65,92,106,114,118,62,20,3,3,68,20,10,8,8,4;6,45,143,70,97,110,106,119,74,19,4,3,92,25,12,9,10,4;5,43,143,59,74,84,108,137,78,20,4,3,88,23,10,7,10,4;6,53,175,76,99,112,119,137,73,20,4,3,83,22,9,6,9,4;5,42,144,69,95,108,127,150,82,22,5,7,88,24,10,7,10,4;6,49,153,72,97,111,122,133,70,22,4,3,64,18,9,6,7,3;";

        // var valuesArray = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
        //     .Select(s => s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
        //         .Select(float.Parse).ToArray()).ToArray();

        var valuesArray = str.Split(';')
            .Select(s => s.Split(',')
                .Select(float.Parse)
                .ToArray())
            .ToArray();

        return new BeltLightResults(valuesArray);
    }
}