using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

/// <summary>
/// 検索空間における状態配置の可視化を担当するクラス
/// PositionServiceから状態位置を取得してPuzzleVisualizerに設定する
/// </summary>
public class SearchSpaceStateManager : MonoBehaviour
{
    [SerializeField] private PuzzleVisualizer puzzleVisualizer;

    private ISearchSpacePositionReader _positionReader;
    private HashSet<PuzzleState> _displayedStates = new HashSet<PuzzleState>();
    private float _edgeThickness;

    [Inject]
    public void Construct(ISearchSpacePositionReader positionReader)
    {
        _positionReader = positionReader;
    }

    /// <summary>
    /// 状態管理の初期化
    /// PositionServiceから状態配置を取得してPuzzleVisualizerに設定する
    /// </summary>
    /// <param name="edgeThickness">エッジの太さ（パズルブロックの厚みを自動調整するために使用）</param>
    public void Initialize(float edgeThickness)
    {
        _edgeThickness = edgeThickness;

        // 自分の担当部分（状態配置）をVisualizerに設定
        var statePositions = _positionReader.StatePositions;
        puzzleVisualizer.AddStates(statePositions, edgeThickness);

        // 表示済み状態を記録
        _displayedStates = new HashSet<PuzzleState>(statePositions.Keys);
    }

    /// <summary>
    /// 表示済み状態をクリアして、ステップ実行用にリセットする
    /// PuzzleVisualizerの描画もクリアする
    /// </summary>
    public void Reset()
    {
        Debug.Log($"[SearchSpaceStateManager] Reset: {_displayedStates.Count}件の状態をクリア");
        _displayedStates.Clear();
        puzzleVisualizer.ClearStates();
    }

    /// <summary>
    /// 新しく発見された状態を増分描画する
    /// </summary>
    /// <param name="newStates">新しく発見された状態のリスト</param>
    public void AddDiscoveredStates(IEnumerable<PuzzleState> newStates)
    {
        // 未表示の状態だけをフィルタリング（1回のループで効率的に処理）
        Dictionary<PuzzleState, Vector3> statesToAdd = new Dictionary<PuzzleState, Vector3>();

        foreach (var state in newStates)
        {
            // 既に表示済みならスキップ
            if (_displayedStates.Contains(state))
                continue;

            // PositionServiceから位置情報を取得
            statesToAdd[state] = _positionReader.StatePositions[state];
            _displayedStates.Add(state);
        }

        // 追加する状態がなければ描画しない
        if (!statesToAdd.Any())
            return;

        // 既存の AddStates() を使って増分描画（重複コード回避）
        puzzleVisualizer.AddStates(statesToAdd, _edgeThickness);
    }
}
