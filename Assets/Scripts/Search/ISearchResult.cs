using System.Collections.Generic;

/// <summary>
/// 探索結果を取得できるインターフェース
/// </summary>
public interface ISearchResult
{
    /// <summary>
    /// 探索済みのデータマップを取得
    /// </summary>
    Dictionary<PuzzleState, PuzzleNodeData> GetResult();
}
