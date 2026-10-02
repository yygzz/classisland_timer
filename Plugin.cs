using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickTimerPlugin.Providers;
using QuickTimerPlugin.Services;
using QuickTimerPlugin.Views;
using QuickTimerPlugin.Views.SettingsPages;

namespace QuickTimerPlugin;

/// <summary>
/// 快速计时器插件入口。
/// </summary>
[PluginEntrance]
public class Plugin : PluginBase
{
    public const string PluginId = "community.quicktimer";

    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 倒计时引擎
        services.AddSingleton<CountdownService>();
        // 到点提醒提供方（官方提醒 V2 API）
        services.AddNotificationProvider<TimerNotificationProvider>();
        // 插件设置页
        services.AddSettingsPage<QuickTimerSettingsPage>();

        // 窗口必须等宿主应用启动后再创建
        AppBase.Current.AppStarted += OnAppStarted;
        AppBase.Current.AppStopping += OnAppStopping;
    }

    private void OnAppStarted(object? sender, EventArgs e)
    {
        // 初始化并加载插件设置（PluginConfigFolder 在此时一定可用）
        QuickTimerSettings.Init(PluginConfigFolder);

        Dispatcher.UIThread.Post(() =>
        {
            var countdown = IAppHost.GetService<CountdownService>();
            var window = new FloatingIconWindow(countdown);
            window.Show();
        });
    }

    private void OnAppStopping(object? sender, EventArgs e)
    {
        QuickTimerSettings.Save();
    }
}
