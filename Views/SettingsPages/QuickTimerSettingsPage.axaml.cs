using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using QuickTimerPlugin.Services;

namespace QuickTimerPlugin.Views.SettingsPages;

[SettingsPageInfo("community.quicktimer.settings", "快速计时器")]
public partial class QuickTimerSettingsPage : SettingsPageBase
{
    public QuickTimerSettingsPage()
    {
        InitializeComponent();
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = QuickTimerSettings.Current;
        ShortMin!.Value = s.ShortMin;
        ShortMax!.Value = s.ShortMax;
        ShortStep!.Value = s.ShortStep;
        MediumMin!.Value = s.MediumMin;
        MediumMax!.Value = s.MediumMax;
        MediumStep!.Value = s.MediumStep;
        LongMin!.Value = s.LongMin;
        LongMax!.Value = s.LongMax;
        LongStep!.Value = s.LongStep;
        PixelsPerStep!.Value = (decimal)s.PixelsPerStep;
        ShowRemainingBadge!.IsChecked = s.ShowRemainingBadge;
        NotifyOnFinish!.IsChecked = s.NotifyOnFinish;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var s = QuickTimerSettings.Current;
        s.ShortMin = ReadInt(ShortMin, s.ShortMin);
        s.ShortMax = ReadInt(ShortMax, s.ShortMax);
        s.ShortStep = ReadInt(ShortStep, s.ShortStep);
        s.MediumMin = ReadInt(MediumMin, s.MediumMin);
        s.MediumMax = ReadInt(MediumMax, s.MediumMax);
        s.MediumStep = ReadInt(MediumStep, s.MediumStep);
        s.LongMin = ReadInt(LongMin, s.LongMin);
        s.LongMax = ReadInt(LongMax, s.LongMax);
        s.LongStep = ReadInt(LongStep, s.LongStep);
        s.PixelsPerStep = PixelsPerStep?.Value is { } p ? (double)p : s.PixelsPerStep;
        s.ShowRemainingBadge = ShowRemainingBadge?.IsChecked ?? true;
        s.NotifyOnFinish = NotifyOnFinish?.IsChecked ?? true;
        QuickTimerSettings.Save();
    }

    private void OnResetPosition(object? sender, RoutedEventArgs e)
    {
        QuickTimerSettings.Current.IconX = null;
        QuickTimerSettings.Current.IconY = null;
        QuickTimerSettings.Save();
    }

    private static int ReadInt(NumericUpDown? control, int fallback)
    {
        var v = control?.Value;
        return v is { } d && d >= 1 ? (int)Math.Round(d) : fallback;
    }
}
