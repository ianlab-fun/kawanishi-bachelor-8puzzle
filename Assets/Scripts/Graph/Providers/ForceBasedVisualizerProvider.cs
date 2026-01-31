using System;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class ForceBasedVisualizerProvider : IVisualizeStrategyProvider
{
    [field: SerializeField]
    public LocalizedString DisplayName { get; private set; }

    [field: SerializeField]
    public LocalizedString Description { get; private set; }

    [SerializeField] private ForceBasedVisualizerConfig config = new ForceBasedVisualizerConfig();

    public IVisualizeStrategy CreateStrategy()
    {
        return new ForceBasedVisualizer(config);
    }
}
