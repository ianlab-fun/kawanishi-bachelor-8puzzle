using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 検索空間における状態位置情報の書き込み用インターフェース
/// 位置情報を設定する責務を持つコンポーネントのみがこのインターフェースに依存する
/// </summary>
public interface ISearchSpacePositionWriter
{
    /// <summary>
    /// 状態位置マッピングを設定する
    /// </summary>
    /// <param name="statePositions">状態とその3D空間上の位置のマッピング</param>
    void SetStatePositions(Dictionary<PuzzleState, Vector3> statePositions);
}
