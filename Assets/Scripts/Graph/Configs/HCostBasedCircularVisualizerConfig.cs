using System;
using UnityEngine;

/// <summary>
/// HCostBasedCircularVisualizer用の設定
/// ヒューリスティックコスト（初期状態からの推定距離）を基準に同心円配置
/// </summary>
[Serializable]
public class HCostBasedCircularVisualizerConfig
{
    [field: SerializeField]
    [field: Tooltip("HCostごとの半径間隔")]
    public float RadiusStep { get; private set; } = 20f;

    [field: SerializeField]
    [field: Tooltip("HCost=0（初期状態）からの開始半径オフセット")]
    public float InitialRadiusOffset { get; private set; } = 0f;

    [field: SerializeField]
    [field: Tooltip("同じ円周上の隣接ノード間の最小円弧長（ユニット）")]
    [field: Range(0f, 50f)]
    public float MinArcSpacing { get; private set; } = 7f;

    [field: SerializeField]
    [field: Tooltip("HCostごとのZ軸ステップ（奥行き方向の間隔）")]
    public float ZDepthStep { get; protected set; } = -20f;

    [field: SerializeField]
    [field: Tooltip("最短経路の配置角度（度）。0度=右方向、90度=上方向")]
    [field: Range(0f, 360f)]
    public float OptimalPathAngle { get; private set; } = 0f;
}
