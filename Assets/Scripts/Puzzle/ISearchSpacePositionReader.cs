using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 検索空間における状態位置情報の読み取り専用インターフェース
/// 位置情報を参照するコンポーネントはこのインターフェースに依存する
/// </summary>
public interface ISearchSpacePositionReader
{
    /// <summary>
    /// 状態位置マッピングを取得する（読み取り専用）
    /// </summary>
    IReadOnlyDictionary<PuzzleState, Vector3> StatePositions { get; }
}
