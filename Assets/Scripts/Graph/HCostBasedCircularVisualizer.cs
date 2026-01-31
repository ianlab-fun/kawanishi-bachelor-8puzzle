using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// HCostベース円形配置可視化戦略（Pure C#実装）
/// ヒューリスティックコスト（初期状態からの推定距離）を基準に同心円配置
/// HCost=0（初期状態）が中心、HCostが大きいほど外側に配置される
/// </summary>
public class HCostBasedCircularVisualizer : IVisualizeStrategy
{
    private readonly HCostBasedCircularVisualizerConfig _config;
    private SearchProgress _progress;

    public HCostBasedCircularVisualizer(HCostBasedCircularVisualizerConfig config)
    {
        _config = config;
    }

    public Dictionary<PuzzleState, Vector3> VisualizeSearchSpace(
        Dictionary<PuzzleState, PuzzleNodeData> searchDataMap,
        PuzzleState initialPuzzleState)
    {
        var puzzleViewMap = new Dictionary<PuzzleState, Vector3>();

        if (!searchDataMap.Any())
            return puzzleViewMap;

        // HCost計算: initialPuzzleStateを基準に計算
        CalculateHCostsIfNeeded(searchDataMap, initialPuzzleState);

        // HCostごとにノードをグループ化
        var nodesByHCost = GroupByHCost(searchDataMap);

        // 各HCostレベルで円周配置
        foreach (var hCostGroup in nodesByHCost.OrderBy(x => x.Key))
        {
            int hCost = hCostGroup.Key;
            var nodes = hCostGroup.Value;

            if (hCost == 0)
            {
                // HCost=0（初期状態）は中心に配置
                foreach (var node in nodes)
                {
                    puzzleViewMap[node] = Vector3.zero;
                    _progress?.Increment();
                }
                continue;
            }

            // 円周配置
            LayoutNodesOnCircle(nodes, hCost, puzzleViewMap);
        }

        return puzzleViewMap;
    }

    public async UniTask<Dictionary<PuzzleState, Vector3>> VisualizeSearchSpaceAsync(
        Dictionary<PuzzleState, PuzzleNodeData> searchDataMap,
        PuzzleState initialPuzzleState,
        SearchProgress progress = null,
        CancellationToken cancellationToken = default)
    {
        _progress = progress;
        await UniTask.SwitchToThreadPool();

        try
        {
            return VisualizeSearchSpace(searchDataMap, initialPuzzleState);
        }
        finally
        {
            _progress = null;
            await UniTask.SwitchToMainThread();
        }
    }

    /// <summary>
    /// HCostごとにノードをグループ化
    /// </summary>
    private Dictionary<int, List<PuzzleState>> GroupByHCost(
        Dictionary<PuzzleState, PuzzleNodeData> searchDataMap)
    {
        var groups = new Dictionary<int, List<PuzzleState>>();

        foreach (var kvp in searchDataMap)
        {
            int hCost = kvp.Value.HCost;

            if (!groups.ContainsKey(hCost))
            {
                groups[hCost] = new List<PuzzleState>();
            }

            groups[hCost].Add(kvp.Key);
        }

        return groups;
    }

    /// <summary>
    /// ノードを円周上に配置
    /// 等間隔配置を維持し、最小円弧間隔が不足する場合は半径を拡大
    /// </summary>
    private void LayoutNodesOnCircle(
        List<PuzzleState> nodes,
        int hCost,
        Dictionary<PuzzleState, Vector3> puzzleViewMap)
    {
        // 基本の半径
        float radius = _config.InitialRadiusOffset + (hCost * _config.RadiusStep);

        // 等間隔配置の角度間隔（固定：360度をノード数で等分）
        float angleStepRad = (2f * Mathf.PI) / nodes.Count;

        // この角度間隔での円弧長を計算
        float arcLength = radius * angleStepRad;

        // もし円弧長が最小間隔未満なら、半径を大きくする
        if (arcLength < _config.MinArcSpacing)
        {
            radius = _config.MinArcSpacing / angleStepRad;
        }

        // 0度から開始（右側から時計回り）
        float startAngleRad = 0f;

        // 全ノードを均等間隔で配置
        for (int i = 0; i < nodes.Count; i++)
        {
            float angleRad = startAngleRad + (angleStepRad * i);

            Vector3 position = new Vector3(
                radius * Mathf.Cos(angleRad),
                radius * Mathf.Sin(angleRad),
                -hCost * _config.ZDepthStep
            );

            puzzleViewMap[nodes[i]] = position;
            _progress?.Increment();
        }
    }

    /// <summary>
    /// 基準状態からの全状態のHCostを計算する
    /// 既存のHCost値に関わらず、常に再計算する
    /// </summary>
    private void CalculateHCostsIfNeeded(
        Dictionary<PuzzleState, PuzzleNodeData> searchDataMap,
        PuzzleState referenceState)
    {
        // 全ノードのHCostを計算（initialStateからの距離）
        foreach (var kvp in searchDataMap)
        {
            int hCost = CalculateManhattanDistance(kvp.Key, referenceState);
            kvp.Value.HCost = hCost;
        }
    }

    /// <summary>
    /// マンハッタン距離（HCost）を計算
    /// 各ブロックの現在位置と基準状態での位置の距離の合計
    /// </summary>
    private static int CalculateManhattanDistance(PuzzleState currentState, PuzzleState referenceState)
    {
        int distance = 0;

        // 3x3のパズル（ブロック番号1-8について計算、0は空きブロックなので無視）
        for (int blockNumber = 1; blockNumber <= 8; blockNumber++)
        {
            BlockPosition? currentPos = null;
            BlockPosition? referencePos = null;

            // 現在の状態からブロックの位置を探す
            for (int row = 0; row < PuzzleState.RowCount; row++)
            {
                for (int col = 0; col < PuzzleState.ColumnCount; col++)
                {
                    var pos = new BlockPosition(row, col);

                    if (currentState[pos] == blockNumber)
                    {
                        currentPos = pos;
                    }

                    if (referenceState[pos] == blockNumber)
                    {
                        referencePos = pos;
                    }
                }
            }

            // 両方見つかったらマンハッタン距離を加算
            if (currentPos.HasValue && referencePos.HasValue)
            {
                distance += Mathf.Abs(currentPos.Value.Row - referencePos.Value.Row) +
                           Mathf.Abs(currentPos.Value.Column - referencePos.Value.Column);
            }
        }

        return distance;
    }
}
