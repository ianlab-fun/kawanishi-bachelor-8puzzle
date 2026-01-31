using System;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// HCostBasedCircularVisualizer用のプロバイダー
/// </summary>
[Serializable]
public class HCostBasedCircularVisualizerProvider : IVisualizeStrategyProvider
{
    [field: SerializeField]
    public LocalizedString DisplayName { get; private set; }

    [field: SerializeField]
    public LocalizedString Description { get; private set; }

    [SerializeField]
    private HCostBasedCircularVisualizerConfig config = new HCostBasedCircularVisualizerConfig();

    public IVisualizeStrategy CreateStrategy()
    {
        return new HCostBasedCircularVisualizer(config);
    }
}
