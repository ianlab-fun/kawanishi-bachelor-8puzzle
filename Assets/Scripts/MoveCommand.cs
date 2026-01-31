using System;
using UnityEngine;

public class MoveCommand : ICommand
{
    private PuzzleState _puzzleState;
    private readonly Vector2Int _direction;
    private Action<Vector2Int> _moveAction;
    
    public MoveCommand(PuzzleState puzzleState, Vector2Int direction, Action<Vector2Int> moveAction)
    {
        _puzzleState = puzzleState;
        _direction = direction;
        _moveAction = moveAction;
    }
    
    public void Execute()
    {
        Move(_direction);
    }

    public void Undo()
    {
        Move(_direction * -1);
    }

    public PuzzleState GetBoardState()
    {
        return _puzzleState;
    }
    
    private void Move(Vector2Int direction)
    {
        _moveAction.Invoke(direction);
    }
}
