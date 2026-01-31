using System;
using UnityEngine;

[Serializable]
public class ForceBasedVisualizerConfig
{
    [Header("Parameters")]
    [field: SerializeField] public int MaxIterations { get; private set; } = 100;
    [field: SerializeField] public float K { get; private set; } = 20f;
    [field: SerializeField] public float InitialTemperature { get; private set; } = 200f;
    [field: SerializeField] public float CoolingRate { get; private set; } = 0.95f;
    [field: SerializeField] public float MinTemperature { get; private set; } = 0.1f;
    [field: SerializeField] public bool IsStub { get; private set; } = true;
    [field: SerializeField] public float Theta { get; private set; } = 0.5f;

    [Header("Layout Parameters")]
    [field: SerializeField] public float InitialAreaSize { get; private set; } = 50f;
    [field: SerializeField] public bool CenterInitialPuzzle { get; private set; } = true;

    [Header("Job System Parameters")]
    [field: SerializeField] public int BatchSize { get; private set; } = 32;
}
