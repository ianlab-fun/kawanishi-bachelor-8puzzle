using System.Collections.Generic;
using R3;

/// <summary>
/// 探索結果の解法を一手ずつ再生する
/// Queue + Puzzleの履歴のハイブリッド方式
/// </summary>
public class SolutionPlayer : PlayerBase
{
    private readonly Puzzle _puzzle;
    private Queue<Puzzle.MoveDirection> _pendingMoves;
    private readonly PuzzleState _initialState;

    public ReadOnlyReactiveProperty<PuzzleState> PuzzleState => _puzzle.State;
    public override bool CanStepForward => _puzzle.HasRedo || (_pendingMoves?.Count > 0);
    public override bool CanStepBack => _puzzle.HasUndo;

    public SolutionPlayer(PuzzleState initialState)
    {
        _initialState = initialState;
        _puzzle = new Puzzle(initialState);
    }

    /// <summary>
    /// 解法をセットして再生準備
    /// </summary>
    public void SetSolution(IEnumerable<Puzzle.MoveDirection> moves)
    {
        _pendingMoves = new Queue<Puzzle.MoveDirection>(moves);
        _state.Value = State.Playing;
    }

    protected override void ExecuteStepForward()
    {
        if (_puzzle.HasRedo)
        {
            _puzzle.RedoCommand();
        }
        else
        {
            _puzzle.TryMoveEmpty(_pendingMoves.Dequeue());
        }
    }

    protected override void ExecuteStepBack()
    {
        _puzzle.UndoCommand();
    }

    protected override void ExecuteReset()
    {
        _puzzle.SetPuzzle(_initialState);
        _pendingMoves?.Clear();
    }
}