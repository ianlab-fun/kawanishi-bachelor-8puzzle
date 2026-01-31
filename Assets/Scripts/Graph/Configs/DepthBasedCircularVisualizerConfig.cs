using System;
using UnityEngine;

/// <summary>
/// DepthBasedCircularVisualizer用の設定
/// </summary>
[Serializable]
public class DepthBasedCircularVisualizerConfig
{
    [field: SerializeField]
    [field: Tooltip("深さごとの半径間隔")]
    public float RadiusStep { get; private set; } = 20f;

    [field: SerializeField]
    [field: Tooltip("深さ0（初期状態）からの開始半径オフセット")]
    public float InitialRadiusOffset { get; private set; } = 0f;

    [field: SerializeField]
    [field: Tooltip("同じ円周上の隣接ノード間の最小円弧長（ユニット）")]
    [field: Range(0f, 50f)]
    public float MinArcSpacing { get; private set; } = 7f;

    [field: SerializeField]
    [field: Tooltip("深さごとのZ軸ステップ（奥行き方向の間隔）")]
    public float ZDepthStep { get; protected set; } = -20f;
}
