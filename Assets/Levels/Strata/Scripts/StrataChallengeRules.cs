using System;

// Pure state: one deadline for the whole challenge, independent of Unity update order.
public sealed class StrataChallengeClock
{
    private double remaining, deadline;
    public bool IsRunning { get; private set; }
    public void Reset(double duration)
    {
        if (double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        remaining = duration;
        IsRunning = false;
    }
    public double Remaining(double now) => IsRunning ? Math.Max(0, deadline - now) : remaining;
    public void Start(double now)
    {
        if (IsRunning || remaining <= 0) return;
        deadline = now + remaining;
        IsRunning = true;
    }
    public void Pause(double now)
    {
        remaining = Remaining(now);
        IsRunning = false;
    }
}

public static class StrataShuffle
{
    // nextIndex(maxExclusive): Unity Random.Range(0, maxExclusive) at runtime.
    public static void FisherYates<T>(T[] items, Func<int, int> nextIndex)
    {
        if (items == null || nextIndex == null) throw new ArgumentNullException();
        for (int i = items.Length - 1; i > 0; i--)
        {
            int j = nextIndex(i + 1);
            if (j < 0 || j > i) throw new ArgumentOutOfRangeException(nameof(nextIndex));
            T saved = items[i]; items[i] = items[j]; items[j] = saved;
        }
    }
}
