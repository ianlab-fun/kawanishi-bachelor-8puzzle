using System;
using System.Collections.Generic;

/// <summary>
/// 探索アルゴリズムの基底クラス
/// 全ての探索アルゴリズムで共通する汎用的な処理を提供
/// </summary>
public abstract class SearchAlgorithmBase
{
    // 完全に共通のフィールド
    protected Dictionary<PuzzleState, PuzzleNodeData> _puzzleDataMap = new Dictionary<PuzzleState, PuzzleNodeData>();
    protected SearchProgress _progress;

    /// <summary>
    /// 探索済みのデータマップを取得
    /// </summary>
    public Dictionary<PuzzleState, PuzzleNodeData> GetResult() => _puzzleDataMap;

    /// <summary>
    /// 親ノードから子ノードデータを作成
    /// </summary>
    protected PuzzleNodeData CreateChildNodeData(PuzzleState parentState)
    {
        PuzzleNodeData parentNodeData = _puzzleDataMap[parentState];
        PuzzleNodeData childNodeData = new PuzzleNodeData();
        childNodeData.SetParent(parentState);
        childNodeData.Depth = parentNodeData.Depth + 1;
        return childNodeData;
    }

    /// <summary>
    /// 2つの状態間に双方向の隣接関係を追加（重複チェック付き）
    /// </summary>
    protected void AddBidirectionalAdjacency(PuzzleState state1, PuzzleState state2)
    {
        PuzzleNodeData nodeData1 = _puzzleDataMap[state1];
        PuzzleNodeData nodeData2 = _puzzleDataMap[state2];

        if (!nodeData1.AdjacentStates.Contains(state2))
        {
            nodeData1.AddAdjacentState(state2);
        }
        if (!nodeData2.AdjacentStates.Contains(state1))
        {
            nodeData2.AddAdjacentState(state1);
        }
    }

    /// <summary>
    /// 指定方向への移動を試み、成功したら新しいパズルを返す
    /// 移動できない場合はnullを返す
    /// </summary>
    protected Puzzle CreateNextPuzzle(Puzzle currentPuzzle, Puzzle.MoveDirection direction)
    {
        var nextPuzzle = currentPuzzle.Clone();
        if (!nextPuzzle.TryMoveEmpty(direction))
            return null;
        return nextPuzzle;
    }
}
