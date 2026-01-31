public class GoalStateHolder
{
    /// <summary>
    /// 現在のパリティに基づいて適切な目標状態を返す
    /// </summary>
    public PuzzleState? GoalState => _isEvenParity ? _evenParityGoal : _oddParityGoal;

    private PuzzleState? _evenParityGoal;
    private PuzzleState? _oddParityGoal;
    private bool _isEvenParity;

    public GoalStateHolder(PuzzleState? evenParityGoal, PuzzleState? oddParityGoal)
    {
        _evenParityGoal = evenParityGoal;
        _oddParityGoal = oddParityGoal;
    }

    /// <summary>
    /// 初期状態を渡してパリティを更新（参照先の切り替え）
    /// </summary>
    public void UpdateParity(PuzzleState startState)
    {
        _isEvenParity = startState.IsEvenParity();
    }

    /// <summary>
    /// 状態のパリティに応じてodd/evenにセット
    /// </summary>
    public void SetPuzzle(PuzzleState state)
    {
        if (state.IsEvenParity())
            _evenParityGoal = state;
        else
            _oddParityGoal = state;
    }
}
