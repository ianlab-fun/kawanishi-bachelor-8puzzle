using UnityEngine;

/// <summary>
/// 検索空間におけるエッジ計算を提供するインターフェース
/// 状態間のエッジを表現するMatrix4x4の計算を担当
/// </summary>
public interface ISearchSpaceEdgeCalculator
{
    /// <summary>
    /// 2点間のエッジを表現するMatrix4x4を計算する
    /// </summary>
    /// <param name="start">開始状態</param>
    /// <param name="end">終了状態</param>
    /// <param name="thickness">エッジの太さ</param>
    /// <returns>エッジの変換行列</returns>
    Matrix4x4 CalculateEdgeMatrix(PuzzleState start, PuzzleState end, float thickness);
}
