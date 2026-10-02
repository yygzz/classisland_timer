using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using QuickTimerPlugin.Services;

namespace QuickTimerPlugin.Views;

/// <summary>
/// 桌面置顶浮动计时图标。
/// 左键拖动 = 多级选时；左键单击 = 暂停/继续；
/// 右键拖动 = 移动图标；右键单击 = 快捷菜单。
/// </summary>
public partial class FloatingIconWindow : Window
{
    private readonly CountdownService _countdown;
    private TimerOverlayWindow? _overlay;

    // 拖动状态
    private bool _leftDown;
    private bool _rightDown;
    private bool _dragSelecting;
    private bool _wasClick;
    private PixelPoint _pressScreenPoint;
    private PixelPoint _pressWindowPos;
    private double _dragDistance;

    // 进度环几何参数（与 axaml 中 Ellipse 保持一致）
    private const double RingRadius = 16.5;
    private const double RingStroke = 3.0;

    private static readonly IBrush PausedBrush = new SolidColorBrush(Color.Parse("#E6555566"));
    private static readonly IBrush NormalBrush = new SolidColorBrush(Color.Parse("#E6222233"));

    public FloatingIconWindow(CountdownService countdown)
    {
        _countdown = countdown;
        InitializeComponent();

        _countdown.Started += OnCountdownChanged;
        _countdown.Stopped += OnCountdownReset;
        _countdown.Finished += OnCountdownReset;
        _countdown.Tick += OnCountdownTick;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Position = ResolveInitialPosition();
        UpdateVisual();
    }

    protected override void OnClosed(EventArgs e)
    {
        _countdown.Started -= OnCountdownChanged;
        _countdown.Stopped -= OnCountdownReset;
        _countdown.Finished -= OnCountdownReset;
        _countdown.Tick -= OnCountdownTick;
        _overlay?.Close();
        _overlay = null;
        base.OnClosed(e);
    }

    // ===== 指针交互 =====

    private void IconBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            _leftDown = true;
            _rightDown = false;
            _wasClick = true;
            _dragDistance = 0;
            _pressScreenPoint = this.PointToScreen(point.Position);
            _pressWindowPos = Position;
            e.Pointer.Capture(IconBorder!);
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            _rightDown = true;
            _leftDown = false;
            _wasClick = true;
            _pressScreenPoint = this.PointToScreen(point.Position);
            _pressWindowPos = Position;
            e.Pointer.Capture(IconBorder!);
        }
        e.Handled = true;
    }

    private void IconBorder_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var screenPoint = this.PointToScreen(point.Position);

        if (_leftDown)
        {
            var delta = screenPoint - _pressScreenPoint;
            _dragDistance = Math.Sqrt(delta.X * (double)delta.X + delta.Y * (double)delta.Y);
            if (_dragDistance > DragSelection.ClickThreshold)
            {
                _wasClick = false;
                if (!_dragSelecting)
                {
                    _dragSelecting = true;
                    ShowOverlay();
                }
                var resolved = DragSelection.Resolve(_dragDistance);
                _overlay?.Update(resolved, _dragDistance);
            }
            e.Handled = true;
        }
        else if (_rightDown)
        {
            var delta = screenPoint - _pressScreenPoint;
            if (Math.Abs(delta.X) + Math.Abs(delta.Y) > 2)
                _wasClick = false;
            Position = new PixelPoint(
                _pressWindowPos.X + delta.X,
                _pressWindowPos.Y + delta.Y);
            e.Handled = true;
        }
    }

    private void IconBorder_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_leftDown)
        {
            _leftDown = false;
            e.Pointer.Capture(null);
            if (_wasClick)
            {
                _countdown.TogglePauseResume();
            }
            else if (_dragSelecting)
            {
                var resolved = DragSelection.Resolve(_dragDistance);
                CloseOverlay();
                if (resolved is { } r)
                    _countdown.Start(TimeSpan.FromMinutes(r.Minutes));
            }
            _dragSelecting = false;
            e.Handled = true;
        }
        else if (_rightDown)
        {
            _rightDown = false;
            e.Pointer.Capture(null);
            if (_wasClick)
                OpenQuickMenu();
            else
                SavePosition();
            e.Handled = true;
        }
    }

    // ===== 快捷菜单 =====

    private void OpenQuickMenu()
    {
        var menu = new ContextMenu();

        var miPause = new MenuItem
        {
            Header = _countdown.State == CountdownState.Paused ? "继续计时" : "暂停计时",
            IsEnabled = _countdown.State != CountdownState.Idle
        };
        miPause.Click += (_, _) => _countdown.TogglePauseResume();

        var miAdd = new MenuItem
        {
            Header = "加 1 分钟",
            IsEnabled = _countdown.State != CountdownState.Idle
        };
        miAdd.Click += (_, _) => _countdown.AddMinutes(1);

        var miSub = new MenuItem
        {
            Header = "减 1 分钟",
            IsEnabled = _countdown.State != CountdownState.Idle
        };
        miSub.Click += (_, _) => _countdown.AddMinutes(-1);

        var miCancel = new MenuItem
        {
            Header = "取消计时",
            IsEnabled = _countdown.State != CountdownState.Idle
        };
        miCancel.Click += (_, _) => _countdown.Cancel();

        var miReset = new MenuItem { Header = "重置图标位置" };
        miReset.Click += (_, _) =>
        {
            QuickTimerSettings.Current.IconX = null;
            QuickTimerSettings.Current.IconY = null;
            QuickTimerSettings.Save();
            Position = ResolveInitialPosition();
        };

        var miHide = new MenuItem { Header = "隐藏图标（重启前）" };
        miHide.Click += (_, _) => Hide();

        menu.Items.Add(miPause);
        menu.Items.Add(miAdd);
        menu.Items.Add(miSub);
        menu.Items.Add(miCancel);
        menu.Items.Add(new Separator());
        menu.Items.Add(miReset);
        menu.Items.Add(miHide);
        menu.Open(IconBorder!);
    }

    // ===== 选时预览层 =====

    private void ShowOverlay()
    {
        _overlay ??= new TimerOverlayWindow();
        _overlay.Update(DragSelection.Resolve(_dragDistance), _dragDistance);
        PositionOverlay();
        if (!_overlay.IsVisible)
            _overlay.Show();
    }

    private void PositionOverlay()
    {
        if (_overlay == null)
            return;
        var icon = Position;
        double ow = _overlay.Width, oh = _overlay.Height;
        var left = icon.X + (int)(Width / 2) - (int)(ow / 2);
        var top = icon.Y - (int)oh - 12;
        if (top < 0)
            top = icon.Y + (int)Height + 12;
        _overlay.Position = new PixelPoint(left, top);
    }

    private void CloseOverlay()
    {
        _overlay?.Hide();
    }

    // ===== 视觉刷新 =====

    private void OnCountdownChanged(object? sender, EventArgs e) => UpdateVisual();

    private void OnCountdownReset(object? sender, EventArgs e) => UpdateVisual();

    private void OnCountdownTick(object? sender, EventArgs e) => UpdateVisual();

    private void UpdateVisual()
    {
        var state = _countdown.State;
        var showBadge = QuickTimerSettings.Current.ShowRemainingBadge;

        BadgeText.Text = state == CountdownState.Idle || !showBadge
            ? ""
            : FormatRemaining(_countdown.Remaining);

        IconBorder!.Background = state == CountdownState.Paused ? PausedBrush : NormalBrush;

        double progress;
        if (state == CountdownState.Idle || _countdown.TotalDuration <= TimeSpan.Zero)
            progress = 0;
        else
            progress = 1.0 - (_countdown.Remaining / _countdown.TotalDuration);
        progress = Math.Clamp(progress, 0.0, 1.0);

        // Avalonia 虚线长度单位为描边宽度的倍数
        var circumference = 2 * Math.PI * RingRadius / RingStroke;
        ProgressEllipse!.StrokeDashArray =
        [
            progress * circumference,
            circumference
        ];
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;
        var totalSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
        if (totalSeconds >= 3600)
            return $"{totalSeconds / 3600}:{totalSeconds % 3600 / 60:00}:{totalSeconds % 60:00}";
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    // ===== 图标位置 =====

    private PixelPoint ResolveInitialPosition()
    {
        var s = QuickTimerSettings.Current;
        if (s.IconX is { } x && s.IconY is { } y)
        {
            var pos = new PixelPoint(x, y);
            var screens = Screens;
            if (screens != null && screens.All.Count > 0)
            {
                // 限制在虚拟屏幕范围内，防止拔除显示器后图标丢失
                var left = int.MaxValue; var top = int.MaxValue;
                var right = int.MinValue; var bottom = int.MinValue;
                foreach (var screen in screens.All)
                {
                    var b = screen.Bounds;
                    left = Math.Min(left, b.X);
                    top = Math.Min(top, b.Y);
                    right = Math.Max(right, b.X + b.Width);
                    bottom = Math.Max(bottom, b.Y + b.Height);
                }
                if (pos.X < left || pos.Y < top || pos.X + Width > right || pos.Y + Height > bottom)
                    return DefaultPosition();
            }
            return pos;
        }
        return DefaultPosition();
    }

    private PixelPoint DefaultPosition()
    {
        var screen = Screens?.Primary;
        if (screen == null)
            return new PixelPoint(200, 200);
        var wa = screen.WorkingArea;
        return new PixelPoint(wa.X + wa.Width - (int)Width - 24, wa.Y + wa.Height - (int)Height - 24);
    }

    private void SavePosition()
    {
        QuickTimerSettings.Current.IconX = Position.X;
        QuickTimerSettings.Current.IconY = Position.Y;
        QuickTimerSettings.Save();
    }
}
