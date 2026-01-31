/// <summary>
/// ステップ実行型探索アルゴリズムの1ステップ分の結果
/// </summary>
public class SearchStepResult
{
    /// <summary>
    /// 今回展開した状態
    /// </summary>
    public PuzzleState ExpandedState { get; }

    /// <summary>
    /// 展開した状態のノードデータ（隣接状態情報を含む）
    /// </summary>
    public PuzzleNodeData ExpandedNodeData { get; }

    public SearchStepResult(
        PuzzleState expandedState,
        PuzzleNodeData expandedNodeData)
    {
        ExpandedState = expandedState;
        ExpandedNodeData = expandedNodeData;
    }
}
