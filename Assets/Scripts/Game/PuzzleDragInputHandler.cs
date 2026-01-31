using R3;
using UnityEngine;

/// <summary>
/// ドラッグ入力を移動方向に変換するハンドラー（アプリケーション層）
/// </summary>
public class PuzzleDragInputHandler
{
    private readonly Puzzle _puzzle;
    private readonly PuzzleGameBlockCreator _blockCreator;
    private readonly RectTransform _containerTransform;

    public PuzzleDragInputHandler(
        Puzzle puzzle,
        PuzzleGameBlockCreator blockCreator,
        RectTransform containerTransform)
    {
        _puzzle = puzzle;
        _blockCreator = blockCreator;
        _containerTransform = containerTransform;
    }

    /// <summary>
    /// ドラッグ移動入力Observable
    /// </summary>
    public Observable<Puzzle.MoveDirection?> DragMove =>
        DraggingPosition.Select(CalculateDragDirection);

    /// <summary>
    /// ドラッグ中の位置Observable
    /// </summary>
    public Observable<Vector2> DraggingPosition => Observable.FromEvent<Vector2>(
        h => _blockCreator.OnBlockDragging += h,
        h => _blockCreator.OnBlockDragging -= h);

    /// <summary>
    /// ドラッグ終了Observable
    /// </summary>
    public Observable<Unit> DragEnded => Observable.FromEvent(
        h => _blockCreator.OnBlockDragEnded += h,
        h => _blockCreator.OnBlockDragEnded -= h);

    /// <summary>
    /// ドラッグ位置から移動方向を計算
    /// </summary>
    private Puzzle.MoveDirection? CalculateDragDirection(Vector2 position)
    {
        PuzzleState currentState = _puzzle.State.CurrentValue;

        for (int row = 0; row < PuzzleState.RowCount; row++)
        {
            for (int column = 0; column < PuzzleState.ColumnCount; column++)
            {
                BlockPosition blockPos = new BlockPosition(row, column);
                BlockNumber number = currentState[blockPos];
                if (number.IsZero()) continue;

                Vector2 size = _blockCreator.GetBlockRect(number).sizeDelta;
                Vector2 pos = (Vector2)_containerTransform.position
                    + _blockCreator.GetLocalPosition(blockPos) - size / 2;
                Rect blockRect = new Rect(pos, size);

                if (blockRect.Contains(position))
                {
                    return CalculateMoveDirection(blockPos, _puzzle.EmptyBlockPosition);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// クリック位置と空ブロック位置から移動方向を計算
    /// </summary>
    private static Puzzle.MoveDirection? CalculateMoveDirection(
        BlockPosition clickedPos,
        BlockPosition emptyPos)
    {
        int rowDiff = clickedPos.Row - emptyPos.Row;
        int colDiff = clickedPos.Column - emptyPos.Column;

        if (rowDiff == 1 && colDiff == 0) return Puzzle.MoveDirection.Down;
        if (rowDiff == -1 && colDiff == 0) return Puzzle.MoveDirection.Up;
        if (rowDiff == 0 && colDiff == 1) return Puzzle.MoveDirection.Right;
        if (rowDiff == 0 && colDiff == -1) return Puzzle.MoveDirection.Left;

        return null;
    }
}
