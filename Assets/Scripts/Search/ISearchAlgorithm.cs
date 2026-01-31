using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 一括実行型の探索アルゴリズム
/// </summary>
public interface ISearchAlgorithm : ISearchResult
{
    /// <summary>
    /// 探索を実行（同期版）
    /// </summary>
    /// <param name="puzzle">探索の開始状態</param>
    /// <param name="goalState">ゴール状態（nullの場合は完全探索）</param>
    void Search(Puzzle puzzle, PuzzleState? goalState = null);

    /// <summary>
    /// 探索を実行（非同期版）
    /// </summary>
    /// <param name="puzzle">探索の開始状態</param>
    /// <param name="goalState">ゴール状態（nullの場合は完全探索）</param>
    /// <param name="progress">プログレス報告用</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    UniTask SearchAsync(
        Puzzle puzzle,
        PuzzleState? goalState,
        SearchProgress progress = null,
        CancellationToken cancellationToken = default
    );
} 