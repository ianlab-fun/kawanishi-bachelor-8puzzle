using R3;

/// <summary>
/// ステップ実行型探索の再生プレイヤー
/// アルゴリズムの状態管理と再生制御を担当
/// </summary>
public class SearchProcessPlayer : PlayerBase
{
    private readonly IStepwiseSearchAlgorithm _algorithm;
    private readonly Puzzle _initialPuzzle;
    private readonly PuzzleState? _goalState;

    /// <summary>
    /// ステップ実行結果の通知（可視化用）
    /// </summary>
    public Observable<SearchStepResult> OnStepExecuted => _algorithm.OnStepExecuted;

    public override bool CanStepForward => !_algorithm.IsCompleted;
    public override bool CanStepBack => false; // 探索の巻き戻しは未サポート

    public SearchProcessPlayer(IStepwiseSearchAlgorithm algorithm, Puzzle initialPuzzle, PuzzleState? goalState)
    {
        _algorithm = algorithm;
        _initialPuzzle = initialPuzzle;
        _goalState = goalState;

        _algorithm.Initialize(initialPuzzle, goalState);
        _state.Value = State.Playing;
    }

    protected override void ExecuteStepForward()
    {
        _algorithm.Step();
    }

    protected override void ExecuteStepBack()
    {
        // 探索の巻き戻しは未サポート（CanStepBack=falseなので呼ばれない）
    }

    protected override void ExecuteReset()
    {
        _algorithm.Reset();
        _algorithm.Initialize(_initialPuzzle, _goalState);
    }
}