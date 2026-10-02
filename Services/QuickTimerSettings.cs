using System.IO;
using System.Text.Json;

namespace QuickTimerPlugin.Services;

/// <summary>
/// 插件设置。序列化保存到插件配置目录（PluginConfigFolder）下的 settings.json。
/// </summary>
public class QuickTimerSettings
{
    private static string? _settingsPath;

    /// <summary>当前生效的设置实例。</summary>
    public static QuickTimerSettings Current { get; set; } = new();

    // ===== 三档计时范围（单位：分钟）=====
    /// <summary>短计时：起始</summary>
    public int ShortMin { get; set; } = 1;
    /// <summary>短计时：结束</summary>
    public int ShortMax { get; set; } = 10;
    /// <summary>短计时：步长</summary>
    public int ShortStep { get; set; } = 1;

    /// <summary>中计时：起始</summary>
    public int MediumMin { get; set; } = 15;
    /// <summary>中计时：结束</summary>
    public int MediumMax { get; set; } = 60;
    /// <summary>中计时：步长</summary>
    public int MediumStep { get; set; } = 5;

    /// <summary>长计时：起始</summary>
    public int LongMin { get; set; } = 75;
    /// <summary>长计时：结束</summary>
    public int LongMax { get; set; } = 120;
    /// <summary>长计时：步长</summary>
    public int LongStep { get; set; } = 15;

    // ===== 交互与显示 =====
    /// <summary>拖动灵敏度：每个档位对应的拖动像素距离（像素/档）。</summary>
    public double PixelsPerStep { get; set; } = 14.0;
    /// <summary>拖动死区（像素），在此距离内视为单击而非选时。</summary>
    public double DeadZone { get; set; } = 16.0;
    /// <summary>计时中是否在图标上显示剩余时间徽标。</summary>
    public bool ShowRemainingBadge { get; set; } = true;
    /// <summary>到点时发送 ClassIsland 提醒。</summary>
    public bool NotifyOnFinish { get; set; } = true;

    // ===== 图标位置（屏幕设备像素）=====
    public int? IconX { get; set; }
    public int? IconY { get; set; }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// 初始化设置：指定插件配置目录并加载设置文件。
    /// </summary>
    public static void Init(string pluginConfigFolder)
    {
        try
        {
            Directory.CreateDirectory(pluginConfigFolder);
            _settingsPath = Path.Combine(pluginConfigFolder, "settings.json");
            if (File.Exists(_settingsPath))
            {
                var loaded = JsonSerializer.Deserialize<QuickTimerSettings>(File.ReadAllText(_settingsPath));
                if (loaded != null)
                    Current = loaded;
            }
        }
        catch
        {
            // 设置加载失败时使用默认设置，不阻塞插件启动
            Current = new QuickTimerSettings();
        }
    }

    /// <summary>保存当前设置。未初始化（无配置目录）时静默跳过。</summary>
    public static void Save()
    {
        if (_settingsPath == null)
            return;
        try
        {
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Current, SerializerOptions));
        }
        catch
        {
            // 保存失败不影响运行
        }
    }
}
