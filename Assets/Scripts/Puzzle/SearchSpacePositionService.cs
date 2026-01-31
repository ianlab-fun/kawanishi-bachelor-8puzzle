using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 検索空間における状態位置マッピングの一元管理サービス（Pure C#）
/// Single Source of Truthとして、全ての位置データとエッジ計算ロジックを保持
/// </summary>
public class SearchSpacePositionService : ISearchSpacePositionReader, ISearchSpaceEdgeCalculator, ISearchSpacePositionWriter
{   
    /// <summary>
    /// 状態位置マッピングを取得する（読み取り専用）
    /// </summary>
    public IReadOnlyDictionary<PuzzleState, Vector3> StatePositions => _statePositions;
    private Dictionary<PuzzleState, Vector3> _statePositions = new Dictionary<PuzzleState, Vector3>();

    /// <summary>
    /// 状態位置マッピングを設定する
    /// </summary>
    /// <param name="statePositions">状態とその3D空間上の位置のマッピング</param>
    void ISearchSpacePositionWriter.SetStatePositions(Dictionary<PuzzleState, Vector3> statePositions)
    {
        _statePositions = statePositions ?? new Dictionary<PuzzleState, Vector3>();
    }

 
    /// <summary>
    /// 2点間のエッジを表現するMatrix4x4を計算する
    /// エッジ計算ロジックの共通化により、コード重複を解消
    /// </summary>
    /// <param name="start">開始位置</param>
    /// <param name="end">終了位置</param>
    /// <param name="thickness">エッジの太さ</param>
    /// <returns>エッジの変換行列</returns>
    public Matrix4x4 CalculateEdgeMatrix(PuzzleState start, PuzzleState end, float thickness)
    {
        Vector3 position = (_statePositions[start] + _statePositions[end]) / 2f;
        Vector3 direction = _statePositions[end] - _statePositions[start];
        Quaternion rotation = Quaternion.identity;

        if (direction != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);
        }

        float length = direction.magnitude;
        Vector3 scale = new Vector3(thickness, length, thickness);

        return Matrix4x4.TRS(position, rotation, scale);
    }
}
