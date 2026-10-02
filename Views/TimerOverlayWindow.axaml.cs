using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using QuickTimerPlugin.Services;

namespace QuickTimerPlugin.Views;

/// <summary>
/// 拖动选时预览层：大号时长预览、三档徽标高亮、非线性刻度轨道。
/// </summary>
public partial class TimerOverlayWindow : Window
{
    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.Parse("#55FFFFFF"));
    private static readonly IBrush TrackHighlightBrush = new SolidColorBrush(Color.Parse("#FF4CC2FF"));

    private const double TrackMargin = 8.0;

    public TimerOverlayWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 更新预览。
    /// </summary>
    /// <param name="resolved">当前拖动距离对应的档位；null 表示处于取消区。</param>
    /// <param name="distancePx">当前拖动距离（像素）。</param>
    public void Update((int Minutes, int Level)? resolved, double distancePx)
    {
        if (resolved is { } r)
        {
            PreviewText!.Text = DragSelection.FormatMinutes(r.Minutes);
            PreviewText.Foreground = new SolidColorBrush(Color.Parse("#FFFFFFFF"));
        }
        else
        {
            PreviewText!.Text = "松开取消";
            PreviewText.Foreground = new SolidColorBrush(Color.Parse("#FFFF6B6B"));
        }

        UpdateChips(resolved?.Level);
        UpdateTrack(resolved);
    }

    private void UpdateChips(int? level)
    {
        Highlight(ChipShort!, ChipShortText!, level == 0);
        Highlight(ChipMedium!, ChipMediumText!, level == 1);
        Highlight(ChipLong!, ChipLongText!, level == 2);
    }

    private static void Highlight(Border chip, TextBlock text, bool active)
    {
        chip.Background = new SolidColorBrush(active ? Color.Parse("#FF4CC2FF") : Color.Parse("#2EFFFFFF"));
        text.Foreground = new SolidColorBrush(active ? Color.Parse("#FF10141F") : Color.Parse("#CCFFFFFF"));
        text.FontWeight = active ? FontWeight.Bold : FontWeight.Normal;
    }

    /// <summary>
    /// 绘制非线性刻度轨道：每个可选档位一个刻度（不等时长间距），当前档高亮。
    /// </summary>
    private void UpdateTrack((int Minutes, int Level)? current)
    {
        var canvas = TrackCanvas!;
        canvas.Children.Clear();

        var options = DragSelection.BuildOptions();
        if (options.Count == 0)
            return;

        var width = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 360;
        var usable = width - TrackMargin * 2;
        var step = options.Count > 1 ? usable / (options.Count - 1) : 0;

        var currentIndex = -1;
        if (current is { } c)
        {
            for (var i = 0; i < options.Count; i++)
                if (options[i].Minutes == c.Minutes && options[i].Level == c.Level)
                {
                    currentIndex = i;
                    break;
                }
        }

        for (var i = 0; i < options.Count; i++)
        {
            var isCurrent = i == currentIndex;
            var x = TrackMargin + step * i;
            var tickHeight = isCurrent ? 14 : 7;
            var tick = new Rectangle
            {
                Width = isCurrent ? 4 : 2,
                Height = tickHeight,
                Fill = isCurrent ? TrackHighlightBrush : TrackBrush,
                [Canvas.LeftProperty] = x - (isCurrent ? 2 : 1),
                [Canvas.TopProperty] = (20 - tickHeight) / 2.0
            };
            canvas.Children.Add(tick);
        }
    }
}
