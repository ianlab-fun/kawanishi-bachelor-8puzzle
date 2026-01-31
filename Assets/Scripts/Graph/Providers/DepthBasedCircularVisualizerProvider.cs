using System;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// DepthBasedCircularVisualizer用のプロバイダー
/// </summary>
[Serializable]
public class DepthBasedCircularVisualizerProvider : IVisualizeStrategyProvider
{
    [field: SerializeField]
    public LocalizedString DisplayName { get; private set; }

    [field: SerializeField]
    public LocalizedString Description { get; private set; }

    [SerializeField]
    private DepthBasedCircularVisualizerConfig config = new DepthBasedCircularVisualizerConfig();

    public IVisualizeStrategy CreateStrategy()
    {
        return new DepthBasedCircularVisualizer(config);
    }
}
