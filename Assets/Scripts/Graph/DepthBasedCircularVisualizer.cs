using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 深さベース円形配置可視化戦略（Pure C#実装）
/// 各深さレベルのノードを円周上に等間隔配置することで重なりを防ぐ
/// </summary>
public class DepthBasedCircularVisualizer : IVisualizeStrategy
{
    private readonly DepthBasedCircularVisualizerConfig _config;
    private SearchProgress _progress;

    public DepthBasedCircularVisualizer(DepthBasedCircularVisualizerConfig config)
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

        // Phase 1: 深さごとにノードをグループ化（探索順を保持）
        var nodesByDepth = GroupByDepth(searchDataMap);

        // Phase 2: 各深さレベルで円周配置
        foreach (var depthGroup in nodesByDepth.OrderBy(x => x.Key))
        {
            int depth = depthGroup.Key;
            var nodes = depthGroup.Value;

            if (depth == 0)
            {
                // 深さ0は中心に配置
                puzzleViewMap[initialPuzzleState] = Vector3.zero;
                _progress?.Increment();
                continue;
            }

            // 円周配置
            LayoutNodesOnCircle(nodes, depth, puzzleViewMap);
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
    /// 深さごとにノードをグループ化
    /// 探索順序（BFS順）を保持
    /// </summary>
    private Dictionary<int, List<PuzzleState>> GroupByDepth(
        Dictionary<PuzzleState, PuzzleNodeData> searchDataMap)
    {
        var groups = new Dictionary<int, List<PuzzleState>>();

        foreach (var kvp in searchDataMap)
        {
            int depth = kvp.Value.Depth;

            if (!groups.ContainsKey(depth))
            {
                groups[depth] = new List<PuzzleState>();
            }

            groups[depth].Add(kvp.Key);
        }

        return groups;
    }

    /// <summary>
    /// ノードを円周上に配置
    /// 等間隔配置を維持し、最小円弧間隔が不足する場合は半径を拡大
    /// </summary>
    private void LayoutNodesOnCircle(
        List<PuzzleState> nodes,
        int depth,
        Dictionary<PuzzleState, Vector3> puzzleViewMap)
    {
        // 基本の半径
        float radius = _config.InitialRadiusOffset + (depth * _config.RadiusStep);

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

        for (int i = 0; i < nodes.Count; i++)
        {
            float angleRad = startAngleRad + (angleStepRad * i);

            Vector3 position = new Vector3(
                radius * Mathf.Cos(angleRad),
                radius * Mathf.Sin(angleRad),
                -depth * _config.ZDepthStep  // 深さに応じたZ座標
            );

            puzzleViewMap[nodes[i]] = position;
            _progress?.Increment();
        }
    }
}
