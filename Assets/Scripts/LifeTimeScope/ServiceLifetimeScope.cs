using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// サービス層の依存性注入を管理するLifetimeScope
/// InputServiceを登録
/// </summary>
public class ServiceLifetimeScope : LifetimeScope
{
    [SerializeField] private PuzzleInputService puzzleInputService;
    [SerializeField] private GameModeInputService gameModeInputService;

    protected override void Configure(IContainerBuilder builder)
    {
        // InputService の登録
        builder.RegisterComponent(puzzleInputService);
        builder.RegisterComponent(gameModeInputService);
    }
}
