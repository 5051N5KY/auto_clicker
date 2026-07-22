using System.Diagnostics;
using AutoKeyPresser.Models;
using AutoKeyPresser.Services;

namespace AutoKeyPresser.Mac.Services;

public sealed class MacAutoPressService : IDisposable
{
    private readonly MacKeyboardInputService _input = new();
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _task;
    private int _running;
    private int _stopRequested;
    private int _requestedReason;
    public bool IsRunning => Volatile.Read(ref _running) == 1;
    public event Action<long>? PressCompleted;
    public event Action? DelayStarted;
    public event Action? Active;
    public event Action<TimeSpan?>? NextPressInChanged;
    public event Action<Exception>? Failed;
    public event Action<StopReason>? Stopped;

    public bool Start(AutoPressOptions options)
    {
        lock (_sync)
        {
            if (IsRunning) return false;
            _cts = new CancellationTokenSource();
            Volatile.Write(ref _requestedReason, (int)StopReason.Manual);
            Volatile.Write(ref _stopRequested, 0);
            Volatile.Write(ref _running, 1);
            _task = RunAsync(options, _cts.Token);
            return true;
        }
    }

    private async Task RunAsync(AutoPressOptions options, CancellationToken token)
    {
        var reason = StopReason.Manual;
        try
        {
            if (options.StartDelayMs > 0)
            {
                DelayStarted?.Invoke();
                await DelayAsync(options.StartDelayMs, token);
            }
            Active?.Invoke();
            var timer = Stopwatch.StartNew();
            long count = 0;
            while (!token.IsCancellationRequested)
            {
                if (AutoPressRules.ReachedLimit(options.LimitMode, count, timer.Elapsed, options.PressCountLimit, options.DurationLimit))
                { reason = StopReason.Limit; break; }
                await _input.PressAsync(options.VirtualKey, options.Modifiers, token);
                PressCompleted?.Invoke(++count);
                if (AutoPressRules.ReachedLimit(options.LimitMode, count, timer.Elapsed, options.PressCountLimit, options.DurationLimit))
                { reason = StopReason.Limit; break; }
                var delay = AutoPressRules.GetNextDelay(options.IntervalMs, options.RandomDeviationEnabled, options.RandomDeviationMs, Random.Shared);
                var wait = Math.Max(1, delay - 40);
                if (options.LimitMode == LimitMode.Duration)
                    wait = (int)Math.Min(wait, Math.Max(1, Math.Ceiling((options.DurationLimit - timer.Elapsed).TotalMilliseconds)));
                await DelayAsync(wait, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { reason = StopReason.Error; Failed?.Invoke(ex); }
        finally
        {
            NextPressInChanged?.Invoke(null);
            Volatile.Write(ref _running, 0);
            var requested = (StopReason)Volatile.Read(ref _requestedReason);
            Stopped?.Invoke(reason is StopReason.Limit or StopReason.Error ? reason : requested);
        }
    }

    private async Task DelayAsync(int milliseconds, CancellationToken token)
    {
        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < milliseconds)
        {
            var remaining = milliseconds - timer.ElapsedMilliseconds;
            NextPressInChanged?.Invoke(TimeSpan.FromMilliseconds(remaining));
            await Task.Delay((int)Math.Min(100, remaining), token);
        }
    }

    public bool Stop(StopReason reason)
    {
        lock (_sync)
        {
            if (!IsRunning || _cts is null || Interlocked.CompareExchange(ref _stopRequested, 1, 0) != 0) return false;
            Interlocked.Exchange(ref _requestedReason, (int)reason);
            _cts.Cancel();
            return true;
        }
    }

    public async Task StopAndWaitAsync(StopReason reason)
    {
        Stop(reason);
        if (_task is not null) await _task;
    }

    public void Dispose() => _cts?.Dispose();
}
