using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// パズルのコアロジック・検索アルゴリズム・可視化戦略の依存性注入を管理するLifetimeScope
/// Puzzle、GoalStateHolder、SearchAlgorithm、VisualizationStrategyを登録
/// </summary>
public class PuzzleLifetimeScope : LifetimeScope
{
    [SerializeReference] public IVisualizeStrategyProvider[] visualizeStrategyProviders;
    [SerializeReference] public ISearchAlgorithm searchAlgorithm;
    [SerializeField] private SerializablePuzzleState initialStartState;
    [SerializeField] private SerializablePuzzleState evenParityGoalState;  // 偶パリティ用
    [SerializeField] private SerializablePuzzleState oddParityGoalState;   // 奇パリティ用

    protected override void Configure(IContainerBuilder builder)
    {
        // Puzzle の登録
        PuzzleState? initialStart = initialStartState.ToPuzzleStateOrNull();
        builder.RegisterInstance(new Puzzle(initialStart.Value));

        // GoalStateHolder の登録（2つの目標状態で初期化）
        PuzzleState? evenGoal = evenParityGoalState.ToPuzzleStateOrNull();
        PuzzleState? oddGoal = oddParityGoalState.ToPuzzleStateOrNull();
        var goalHolder = new GoalStateHolder(evenGoal, oddGoal);
        goalHolder.UpdateParity(initialStart.Value);  // 初期状態のパリティで参照先を設定
        builder.RegisterInstance(goalHolder);

        // SearchAlgorithm の登録
        builder.RegisterInstance(searchAlgorithm);

        // VisualizationStrategyHolderを生成し、両方のインターフェースで登録
        var strategyHolder = new VisualizationStrategyHolder(visualizeStrategyProviders);
        builder.RegisterInstance<IVisualizationStrategyProvider>(strategyHolder);
        builder.RegisterInstance<IVisualizationStrategyController>(strategyHolder);

        // SearchSpacePositionService の登録（シングルトン）
        // 3つのインターフェースで登録して、用途に応じて使い分ける
        builder.Register<SearchSpacePositionService>(Lifetime.Singleton)
            .As<ISearchSpacePositionReader, ISearchSpaceEdgeCalculator, ISearchSpacePositionWriter>();
        
        // Model の登録
        builder.Register<CameraDistanceModel>(Lifetime.Singleton);
    }
}