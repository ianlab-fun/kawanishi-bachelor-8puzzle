using System;
using R3;

/// <summary>
/// ステップ実行可能な探索アルゴリズムのインターフェース
/// 探索を1ノードずつ展開し、リアルタイムで可視化できる
/// </summary>
public interface IStepwiseSearchAlgorithm : ISearchResult
{
    /// <summary>
    /// 探索の初期化
    /// </summary>
    /// <param name="initialPuzzle">探索の開始状態</param>
    /// <param name="goalState">ゴール状態（nullの場合は完全探索）</param>
    void Initialize(Puzzle initialPuzzle, PuzzleState? goalState = null);

    /// <summary>
    /// 1ノードを展開して結果を返す
    /// </summary>
    /// <returns>展開結果</returns>
    /// <exception cref="InvalidOperationException">Initialize()前、またはIsCompleted後に呼び出された場合</exception>
    SearchStepResult Step();

    /// <summary>
    /// 探索が完了したかどうか
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// ステップ実行時に通知されるObservable
    /// </summary>
    Observable<SearchStepResult> OnStepExecuted { get; }

    /// <summary>
    /// 状態をリセットして初期化前の状態に戻す
    /// </summary>
    void Reset();
}
