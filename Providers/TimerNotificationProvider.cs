using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using QuickTimerPlugin.Services;

namespace QuickTimerPlugin.Providers;

/// <summary>
/// 快速计时器到点提醒提供方（ClassIsland 提醒 V2 API）。
/// </summary>
[NotificationProviderInfo("9d4b1a76-5c2e-4f8a-b31d-7e06c85a19f4", "快速计时器", "快速计时器插件的到点提醒。")]
public class TimerNotificationProvider : NotificationProviderBase
{
    private readonly CountdownService _countdown;

    public TimerNotificationProvider(CountdownService countdown) : base()
    {
        _countdown = countdown;
        _countdown.Finished += OnCountdownFinished;
    }

    private void OnCountdownFinished(object? sender, EventArgs e)
    {
        if (!QuickTimerSettings.Current.NotifyOnFinish)
            return;

        var durationText = _countdown.TotalDescription;
        var request = new NotificationRequest
        {
            MaskContent = NotificationContent.CreateTwoIconsMask($"计时结束：{durationText}"),
            OverlayContent = NotificationContent.CreateSimpleTextContent($"快速计时器\n{durationText}的计时已经结束。")
        };
        ShowNotification(request);
    }
}
