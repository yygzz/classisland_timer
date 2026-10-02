using Avalonia.Threading;

namespace QuickTimerPlugin.Services;

public enum CountdownState
{
    Idle,
    Running,
    Paused
}

/// <summary>
/// 倒计时引擎（基于 DateTimeOffset 防漂移）。需在 UI 线程上调用。
/// </summary>
public class CountdownService
{
    private DispatcherTimer? _timer;
    private TimeSpan _pausedRemaining = TimeSpan.Zero;

    /// <summary>开始了一次新的计时。</summary>
    public event EventHandler? Started;
    /// <summary>计时被取消或重置。</summary>
    public event EventHandler? Stopped;
    /// <summary>计时到点结束。</summary>
    public event EventHandler? Finished;
    /// <summary>周期性状态刷新（约 4 次/秒）。</summary>
    public event EventHandler? Tick;

    public CountdownState State { get; private set; } = CountdownState.Idle;

    /// <summary>本次计时的目标结束时刻（暂停期间为暂停前时刻）。</summary>
    public DateTimeOffset EndTime { get; private set; } = DateTimeOffset.Now;

    /// <summary>本次计时的总时长（到点后保留，用于提示文案）。</summary>
    public TimeSpan TotalDuration { get; private set; } = TimeSpan.Zero;

    /// <summary>剩余时间。空闲时为零。</summary>
    public TimeSpan Remaining => State switch
    {
        CountdownState.Running => EndTime - DateTimeOffset.Now,
        CountdownState.Paused => _pausedRemaining,
        _ => TimeSpan.Zero
    };

    /// <summary>总时长描述（用于到点通知文案）。</summary>
    public string TotalDescription => DragSelection.FormatMinutes((int)Math.Round(TotalDuration.TotalMinutes));

    public void Start(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return;
        TotalDuration = duration;
        EndTime = DateTimeOffset.Now + duration;
        State = CountdownState.Running;
        EnsureTimer();
        _timer?.Start();
        Started?.Invoke(this, EventArgs.Empty);
        Tick?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        if (State != CountdownState.Running)
            return;
        _pausedRemaining = EndTime - DateTimeOffset.Now;
        if (_pausedRemaining < TimeSpan.Zero)
            _pausedRemaining = TimeSpan.Zero;
        State = CountdownState.Paused;
        _timer?.Stop();
        Tick?.Invoke(this, EventArgs.Empty);
    }

    public void Resume()
    {
        if (State != CountdownState.Paused)
            return;
        if (_pausedRemaining <= TimeSpan.Zero)
        {
            Finish();
            return;
        }
        EndTime = DateTimeOffset.Now + _pausedRemaining;
        State = CountdownState.Running;
        EnsureTimer();
        _timer?.Start();
        Tick?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>暂停/继续切换。空闲时不产生效果。</summary>
    public void TogglePauseResume()
    {
        switch (State)
        {
            case CountdownState.Running:
                Pause();
                break;
            case CountdownState.Paused:
                Resume();
                break;
        }
    }

    /// <summary>增加或减少剩余分钟数（下限 0，到 0 时立即结束）。</summary>
    public void AddMinutes(int minutes)
    {
        if (State == CountdownState.Idle)
            return;
        var delta = TimeSpan.FromMinutes(minutes);
        if (State == CountdownState.Running)
        {
            EndTime += delta;
            if (EndTime <= DateTimeOffset.Now)
                Finish();
            else
                Tick?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _pausedRemaining += delta;
            if (_pausedRemaining < TimeSpan.Zero)
                _pausedRemaining = TimeSpan.Zero;
            Tick?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Cancel()
    {
        if (State == CountdownState.Idle)
            return;
        State = CountdownState.Idle;
        _timer?.Stop();
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    private void Finish()
    {
        State = CountdownState.Idle;
        _timer?.Stop();
        Finished?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureTimer()
    {
        if (_timer != null)
            return;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _timer.Tick += (_, _) =>
        {
            if (State != CountdownState.Running)
                return;
            if (Remaining <= TimeSpan.Zero)
            {
                Finish();
                return;
            }
            Tick?.Invoke(this, EventArgs.Empty);
        };
    }
}
