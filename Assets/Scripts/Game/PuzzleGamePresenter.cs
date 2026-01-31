using System.Linq;
using R3;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

/// <summary>
/// パズルゲームモードのPresenter
/// </summary>
/// <remarks>
/// パズル操作に関する全てのロジックを担当:
/// - ゲーム用パズルの管理
/// - 入力処理（キーボード、クリック、ドラッグ）
/// - Undo/Redo
/// - Finder連携
/// - View更新
/// </remarks>
public class PuzzleGamePresenter : MonoBehaviour
{
    // View参照
    [SerializeField] private PuzzleView puzzleView;
    [SerializeField] private PuzzleFinderView finderView;
    [SerializeField] private PuzzleGameBlockCreator puzzleGameBlockCreator;
    [SerializeField] private CameraFollow cameraFollow;

    // DI注入
    private Puzzle _puzzle;
    private ISearchSpacePositionReader _positionReader;

    // ゲーム用パズル（セッション中に保持）
    private Puzzle _gamePuzzle;
    private PuzzleState _initialState;  // 初期状態（リセット用）
    private readonly Subject<PuzzleState> _gamePuzzleStateSubject = new();

    // ドラッグ入力ハンドラー
    private PuzzleDragInputHandler _dragInputHandler;

    // Reverse Toggle状態
    private bool _isReverse;

    /// <summary>
    /// ゲーム用パズル状態のObservable
    /// Stateがカメラ追従・アニメーション購読に使用
    /// </summary>
    public Observable<PuzzleState> GamePuzzleState => _gamePuzzleStateSubject;

    [Inject]
    public void Construct(
        Puzzle puzzle,
        ISearchSpacePositionReader positionReader)
    {
        _puzzle = puzzle;
        _positionReader = positionReader;
    }

    /// <summary>
    /// セッション開始（OnEnter時）
    /// </summary>
    /// <returns>購読管理用のCompositeDisposable</returns>
    public CompositeDisposable StartSession(PuzzleInputService inputService)
    {
        var disposables = new CompositeDisposable();

        // ゲーム用パズルを初期化（毎回新規作成）
        if (_gamePuzzle == null)
        {
            _gamePuzzle = new Puzzle(_puzzle.State.CurrentValue);
            _initialState = _puzzle.State.CurrentValue;
        }

        // パズル状態をSubjectに中継
        _gamePuzzle.State
            .Subscribe(state => _gamePuzzleStateSubject.OnNext(state))
            .AddTo(disposables);

        // ドラッグ入力ハンドラーを生成
        _dragInputHandler = new PuzzleDragInputHandler(
            _gamePuzzle,
            puzzleGameBlockCreator,
            puzzleGameBlockCreator.GetComponent<RectTransform>()
        );

        // 入力購読を設定
        SetupInputSubscriptions(disposables, inputService);

        // View更新購読を設定
        SetupViewSubscriptions(disposables);

        // 全ての購読設定が完了したので、現在の状態を発火
        _gamePuzzleStateSubject.OnNext(_gamePuzzle.State.CurrentValue);

        return disposables;
    }

    /// <summary>
    /// 入力関連の購読設定
    /// </summary>
    private void SetupInputSubscriptions(CompositeDisposable disposables, PuzzleInputService inputService)
    {
        // Reverse Toggle値変更
        inputService.GetReverseToggleObservable()
            .Subscribe(isReverse => _isReverse = isReverse)
            .AddTo(disposables);

        // キーボード移動入力（Reverse対応）
        inputService.GetPuzzleMoveObservable()
            .Subscribe(direction =>
            {
                var actualDirection = _isReverse ? ReverseDirection(direction) : direction;
                _gamePuzzle.TryMoveEmpty(actualDirection);
            })
            .AddTo(disposables);

        // クリック入力
        puzzleGameBlockCreator.GetBlockClickObservable()
            .Select(CalculateClickMove)
            .Where(result => result.HasValue)
            .Select(state => state.Value)
            .Subscribe(move =>
            {
                // 複数回移動を実行
                for (int i = 0; i < move.count; i++)
                {
                    _gamePuzzle.TryMoveEmpty(move.direction);
                }
            })
            .AddTo(disposables);

        // ドラッグ入力
        _dragInputHandler.DragMove
            .Where(direction => direction.HasValue)
            .Select(state => state.Value)
            .Subscribe(direction => _gamePuzzle.TryMoveEmpty(direction))
            .AddTo(disposables);

        // ドラッグ中の空ブロック位置更新
        _dragInputHandler.DraggingPosition
            .Subscribe(position => puzzleView.UpdateEmptyBlockPosition(position))
            .AddTo(disposables);

        // ドラッグ終了時の空ブロック位置リセット
        _dragInputHandler.DragEnded
            .Subscribe(_ => puzzleView.ResetEmptyBlockPosition(_gamePuzzle.State.CurrentValue))
            .AddTo(disposables);

        // Undo
        inputService.GetUndoObservable()
            .Subscribe(_ => _gamePuzzle.UndoCommand())
            .AddTo(disposables);

        // Redo
        inputService.GetRedoObservable()
            .Subscribe(_ => _gamePuzzle.RedoCommand())
            .AddTo(disposables);

        // 初期状態に戻す
        inputService.GetResetObservable()
            .Subscribe(_ =>
            {
                _gamePuzzle.SetPuzzle(_initialState);
                puzzleView.SetPositionImmediate(_initialState);
            })
            .AddTo(disposables);

        // Finder適用
        finderView.AppliedState
            .Where(state => state.HasValue)
            .Select(state => state.Value)
            .Subscribe(state =>
            {
                if (!_positionReader.StatePositions.TryGetValue(state, out Vector3 _))
                    return;
                
                _gamePuzzle.SetPuzzle(state);
                puzzleView.SetPositionImmediate(state);
            })
            .AddTo(disposables);
    }

    /// <summary>
    /// View更新関連の購読設定
    /// </summary>
    private void SetupViewSubscriptions(CompositeDisposable disposables)
    {
        // ブロックの操作可能状態を更新（半透明化）
        _gamePuzzle.State
            .Subscribe(state => puzzleGameBlockCreator.UpdateBlockInteractivity(state))
            .AddTo(disposables);
    }

    /// <summary>
    /// ゲームパズルをリセット（初期状態変更時に呼び出し）
    /// </summary>
    public void ResetGamePuzzle() => _gamePuzzle = null;

    /// <summary>
    /// 現在のパズル状態をFinderに反映
    /// </summary>
    public void StateToFinder()
    {
        finderView.SetPuzzle(_gamePuzzle.State.CurrentValue);
    }

    /// <summary>
    /// 現在のパズル状態を検索し、カメラを移動
    /// </summary>
    public void FindState()
    {
        Vector3 position = _positionReader.StatePositions[_gamePuzzle.State.CurrentValue];
        cameraFollow.MoveToPosition(position);
    }

    /// <summary>
    /// 前の状態から通常のユーザー操作（1手のSwap）で到達可能な変更かチェック
    /// </summary>
    public static bool IsUserOperationChange(PuzzleState previous, PuzzleState current)
    {
        var emptyPos = previous.EmptyBlockPosition;

        // 上下左右の4方向
        BlockPosition[] adjacentPositions = new[]
        {
            new BlockPosition(emptyPos.Row - 1, emptyPos.Column),
            new BlockPosition(emptyPos.Row + 1, emptyPos.Column),
            new BlockPosition(emptyPos.Row, emptyPos.Column - 1),
            new BlockPosition(emptyPos.Row, emptyPos.Column + 1)
        };

        // いずれかの方向にSwapした結果がcurrentと一致するかチェック
        return adjacentPositions
            .Select(previous.Swap)
            .Any(swappedState => swappedState.HasValue && swappedState.Value.Equals(current));
    }

    /// <summary>
    /// 移動方向を反転させる
    /// </summary>
    private static Puzzle.MoveDirection ReverseDirection(Puzzle.MoveDirection direction)
    {
        return direction switch
        {
            Puzzle.MoveDirection.Up => Puzzle.MoveDirection.Down,
            Puzzle.MoveDirection.Down => Puzzle.MoveDirection.Up,
            Puzzle.MoveDirection.Left => Puzzle.MoveDirection.Right,
            Puzzle.MoveDirection.Right => Puzzle.MoveDirection.Left,
            _ => direction
        };
    }

    /// <summary>
    /// クリック位置から移動方向と回数を計算
    /// </summary>
    private (Puzzle.MoveDirection direction, int count)? CalculateClickMove(BlockNumber clickedNumber)
    {
        if (clickedNumber.IsZero()) return null;

        BlockPosition clickedPos = _gamePuzzle.State.CurrentValue.FindNumberBlockPosition(clickedNumber);
        BlockPosition emptyPos = _gamePuzzle.EmptyBlockPosition;

        int rowDiff = System.Math.Abs(clickedPos.Row - emptyPos.Row);
        int colDiff = System.Math.Abs(clickedPos.Column - emptyPos.Column);

        // 斜め移動は許可しない
        if (rowDiff != 0 && colDiff != 0) return null;

        // 縦方向の移動
        if (rowDiff > 0)
        {
            Puzzle.MoveDirection direction = clickedPos.Row > emptyPos.Row
                ? Puzzle.MoveDirection.Down
                : Puzzle.MoveDirection.Up;
            return (direction, rowDiff);
        }

        // 横方向の移動
        if (colDiff > 0)
        {
            Puzzle.MoveDirection direction = clickedPos.Column > emptyPos.Column
                ? Puzzle.MoveDirection.Right
                : Puzzle.MoveDirection.Left;
            return (direction, colDiff);
        }

        return null;
    }
}
