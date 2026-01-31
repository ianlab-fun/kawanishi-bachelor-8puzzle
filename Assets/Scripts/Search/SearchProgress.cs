using System;

/// <summary>
/// 探索の進捗状況を表すクラス
/// Increment()でカウントアップと通知を同時に行う
/// </summary>
public class SearchProgress
{
    private readonly Action<SearchProgress> _onReport;

    /// <summary>探索済み状態数</summary>
    public int ExploredCount { get; private set; }

    /// <summary>最大予測数</summary>
    public int MaxEstimate { get; }

    /// <summary>進捗率（%）</summary>
    public float Rate => (float)ExploredCount / MaxEstimate;

    public SearchProgress(Action<SearchProgress> onReport, int maxEstimate = Puzzle.TotalReachableStates)
    {
        _onReport = onReport;
        MaxEstimate = maxEstimate;
        ExploredCount = 0;
    }

    public void Increment()
    {
        ExploredCount++;
        _onReport?.Invoke(this);
    }

    public void Increment(int amount)
    {
        ExploredCount += amount;
        _onReport?.Invoke(this);
    }

    /// <summary>
    /// 100%完了として通知
    /// </summary>
    public void Complete()
    {
        ExploredCount = MaxEstimate;
        _onReport?.Invoke(this);
    }
}
