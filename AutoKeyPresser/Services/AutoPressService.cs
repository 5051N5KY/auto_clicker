using System.Diagnostics;
using AutoKeyPresser.Models;

namespace AutoKeyPresser.Services;

public sealed class AutoPressService : IDisposable
{
    private readonly KeyboardInputService _input;
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private int _running;
    private int _requestedReason;
    private int _stopRequested;
    public bool IsRunning => Volatile.Read(ref _running) == 1;
    public event Action<long>? PressCompleted;
    public event Action? DelayStarted;
    public event Action? Active;
    public event Action<Exception>? Failed;
    public event Action<TimeSpan?>? NextPressInChanged;
    public event Action<StopReason>? Stopped;

    public AutoPressService(KeyboardInputService input) => _input = input;

    public bool Start(AutoPressOptions options)
    {
        lock (_sync)
        {
            if (IsRunning) return false;
            _cts = new CancellationTokenSource();
            Volatile.Write(ref _requestedReason, (int)StopReason.Manual);
            Volatile.Write(ref _stopRequested, 0);
            Volatile.Write(ref _running, 1);
            _runTask = RunAsync(options, _cts.Token);
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
                await DelayWithCountdownAsync(options.StartDelayMs, token);
            }
            Active?.Invoke();
            var timer = Stopwatch.StartNew();
            long count = 0;
            var random = Random.Shared;
            while (!token.IsCancellationRequested)
            {
                if (AutoPressRules.ReachedLimit(options.LimitMode, count, timer.Elapsed, options.PressCountLimit, options.DurationLimit))
                {
                    reason = StopReason.Limit;
                    break;
                }
                await _input.PressAsync(options.VirtualKey, token);
                count++;
                PressCompleted?.Invoke(count);
                if (AutoPressRules.ReachedLimit(options.LimitMode, count, timer.Elapsed, options.PressCountLimit, options.DurationLimit))
                {
                    reason = StopReason.Limit;
                    break;
                }
                var delay = AutoPressRules.GetNextDelay(options.IntervalMs, options.RandomDeviationEnabled, options.RandomDeviationMs, random);
                var waitMs = Math.Max(1, delay - 40);
                if (options.LimitMode == LimitMode.Duration)
                {
                    var remainingMs = options.DurationLimit.TotalMilliseconds - timer.Elapsed.TotalMilliseconds;
                    if (remainingMs <= 0)
                    {
                        reason = StopReason.Limit;
                        break;
                    }
                    waitMs = (int)Math.Min(waitMs, Math.Ceiling(remainingMs));
                }
                await DelayWithCountdownAsync(waitMs, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            reason = StopReason.Error;
            Failed?.Invoke(ex);
        }
        finally
        {
            NextPressInChanged?.Invoke(null);
            Volatile.Write(ref _running, 0);
            var requested = (StopReason)Volatile.Read(ref _requestedReason);
            Stopped?.Invoke(reason is StopReason.Limit or StopReason.Error ? reason : requested);
        }
    }

    private async Task DelayWithCountdownAsync(int milliseconds, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            var remaining = milliseconds - stopwatch.ElapsedMilliseconds;
            if (remaining <= 0) break;
            NextPressInChanged?.Invoke(TimeSpan.FromMilliseconds(remaining));
            await Task.Delay((int)Math.Min(100, remaining), token);
        }
        NextPressInChanged?.Invoke(TimeSpan.Zero);
    }

    public bool Stop(StopReason reason)
    {
        CancellationTokenSource? cts;
        lock (_sync)
        {
            if (!IsRunning || _cts is null || Interlocked.CompareExchange(ref _stopRequested, 1, 0) != 0) return false;
            cts = _cts;
        }
        Interlocked.Exchange(ref _requestedReason, (int)reason);
        cts.Cancel();
        return true;
    }

    public async Task StopAndWaitAsync(StopReason reason)
    {
        Stop(reason);
        var task = _runTask;
        if (task is not null) await task;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
