namespace QuickTimerPlugin.Services;

/// <summary>
/// 多级拖动选时核心：把拖动距离映射为时长档位。
/// 三档（短/中/长）各自的范围与步长不同（非线性刻度），
/// 每个档位占据相同的拖动像素（灵敏度可调），拖得越远时长越长。
/// </summary>
public static class DragSelection
{
    /// <summary>判定为单击（而非拖动）的最大移动像素。</summary>
    public const double ClickThreshold = 6.0;

    /// <summary>档位显示名（与 LevelIndex 对应）。</summary>
    public static readonly string[] LevelNames = ["短", "中", "长"];

    /// <summary>
    /// 构建当前设置下所有可选时长（已按档位顺序排列）。
    /// </summary>
    public static List<(int Minutes, int Level)> BuildOptions()
    {
        var s = QuickTimerSettings.Current;
        var result = new List<(int, int)>();
        result.AddRange(EnumerateLevel(s.ShortMin, s.ShortMax, s.ShortStep, 0));
        result.AddRange(EnumerateLevel(s.MediumMin, s.MediumMax, s.MediumStep, 1));
        result.AddRange(EnumerateLevel(s.LongMin, s.LongMax, s.LongStep, 2));
        return result;
    }

    private static IEnumerable<(int, int)> EnumerateLevel(int min, int max, int step, int level)
    {
        if (step <= 0 || max < min)
            yield break;
        for (var m = Math.Max(1, min); m <= max; m += step)
            yield return (m, level);
    }

    /// <summary>
    /// 将拖动距离（像素）解析为时长档位。
    /// 返回 null 表示处于取消区（拖回原点附近）。
    /// </summary>
    public static (int Minutes, int Level)? Resolve(double distancePx)
    {
        var options = BuildOptions();
        if (options.Count == 0)
            return null;

        var s = QuickTimerSettings.Current;
        var deadZone = Math.Max(4.0, s.DeadZone);
        if (distancePx <= deadZone)
            return null; // 拖回原点 = 取消

        var pxPerStep = Math.Max(2.0, s.PixelsPerStep);
        var index = (int)((distancePx - deadZone) / pxPerStep);
        index = Math.Clamp(index, 0, options.Count - 1);
        return options[index];
    }

    /// <summary>格式化时长（分钟）为人可读文本。</summary>
    public static string FormatMinutes(int minutes)
    {
        if (minutes < 60)
            return $"{minutes} 分钟";
        var h = minutes / 60;
        var m = minutes % 60;
        return m == 0 ? $"{h} 小时" : $"{h} 小时 {m} 分钟";
    }
}
