using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;

public class PathSearchPresenter : MonoBehaviour
{
    // インスペクタ設定
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Slider executionIntervalSlider;
    [SerializeField] private TextMeshProUGUI executionIntervalText;
    [SerializeField] private LocalizedString executionIntervalFormat;
    [SerializeField] private Toggle highlightSearchPathToggle;
    [SerializeField] private SearchSpaceEdgeManager edgeManager;
    [SerializeField] private TextMeshProUGUI autoSolveButtonText;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;
    [SerializeField] private Button resetButton;

    [Header("Button Text Localization")]
    [SerializeField] private LocalizedString btnExecuteText;
    [SerializeField] private LocalizedString btnPauseText;
    [SerializeField] private LocalizedString btnResumeText;
    [SerializeField] private LocalizedString btnResetText;

    private Puzzle _puzzle; // 初期状態を保持するだけ（変更しない）
    private SolutionPlayer _solutionPlayer; // 解法再生用

    // 解法再生中のPuzzle状態を公開（購読可能）
    private Subject<PuzzleState> _searchPuzzleStateSubject = new Subject<PuzzleState>();
    public Observable<PuzzleState> SearchPuzzleState => _searchPuzzleStateSubject;

    // SolutionPlayerの状態購読を管理
    private CompositeDisposable _solutionPlayerDisposables;

    private GoalStateHolder _goalStateHolder;

    // 自動探索のキャンセル用
    private CancellationTokenSource _autoSolveCts;

    [Inject]
    public void Construct(Puzzle puzzle, GoalStateHolder goalStateHolder)
    {
        _puzzle = puzzle;
        _goalStateHolder = goalStateHolder;
    }

    private async void Awake()
    {
        // スライダー値をローカライズ表示（英語: 0.50s、日本語: 0.50秒）
        executionIntervalSlider.onValueChanged.AddListener(UpdateIntervalTextAsync);
        executionIntervalText.text = await executionIntervalFormat.GetLocalizedStringAsync(executionIntervalSlider.value);

        // ボタンテキストの初期値
        autoSolveButtonText.text = await btnExecuteText.GetLocalizedStringAsync();

        undoButton.onClick.AddListener(UndoMove);
        redoButton.onClick.AddListener(RedoMove);
        resetButton.onClick.AddListener(ResetToInitialState);

        // 初期状態ではボタンを無効化
        UpdateUndoRedoButtonState();
    }

    private async void UpdateIntervalTextAsync(float value)
    {
        executionIntervalText.text = await executionIntervalFormat.GetLocalizedStringAsync(value);
    }
    
    /// <summary>
    /// 一手戻る（自動探索用Undo）
    /// </summary>
    private void UndoMove() => _solutionPlayer?.StepBack();

    /// <summary>
    /// 一手進む（自動探索用Redo）
    /// </summary>
    private void RedoMove() => _solutionPlayer?.StepForward();
    
    /// <summary>
    /// Undo/Redoボタンの有効/無効を更新
    /// </summary>
    private void UpdateUndoRedoButtonState()
    {
        undoButton.interactable = _solutionPlayer?.CanStepBack ?? false;
        redoButton.interactable = _solutionPlayer?.CanStepForward ?? false;
    }
    
    /// <summary>
    /// ボタンクリック時の統合ハンドラ
    /// 現在の状態に応じて適切なアクションを実行
    /// </summary>
    public void OnAutoSolveButtonClicked()
    {
        if (_solutionPlayer == null)
        {
            StartAutoSolveWithCurrentState();
            return;
        }
        switch (_solutionPlayer.CurrentState.CurrentValue)
        {
            case PlayerBase.State.Idle:
                StartAutoSolveWithCurrentState();
                break;
            case PlayerBase.State.Playing:
                PauseAutoSolve();
                break;
            case PlayerBase.State.Paused:
                ResumeAutoSolve();
                break;
            case PlayerBase.State.Completed:
                ResetToInitialState();
                break;
        }
    }

    private void StartAutoSolveWithCurrentState()
    {
        // ゴール状態が設定されていない場合は警告を出して中止
        if (!_goalStateHolder.GoalState.HasValue)
        {
            Debug.LogWarning("自動求解にはゴール状態が必要です。Finderビューでゴール状態を設定してください。");
            return;
        }

        ISearchAlgorithm algorithm = dropdown.value switch
        {
            0 => new AStarSearch(),
            1 => new BreadthFirstSearch(),
            2 => new DepthFirstSearch(),
            _ => new AStarSearch()
        };
        
        StartAutoSolve(algorithm);
    }

    private void StartAutoSolve(ISearchAlgorithm algorithm)
    {
        var initialState = _puzzle.State.CurrentValue;

        // SolutionPlayerを作成
        _solutionPlayer = new SolutionPlayer(initialState);

        SubscribeSolutionPlayer();

        // 初期状態を即座に発火
        _searchPuzzleStateSubject.OnNext(initialState);

        // 探索を実行（一時的なPuzzleを使用）
        var tempPuzzle = new Puzzle(initialState);
        algorithm.Search(tempPuzzle, _goalStateHolder.GoalState);
        var dataMap = algorithm.GetResult();

        // 経路を復元（移動方向と状態を同時に取得）
        var (moveDirections, statePath) = ReconstructPath(dataMap);

        // 経路可視化データを常に準備（表示はrouteEdgeToggleで制御）
        edgeManager.VisualizeRoutes(statePath, dataMap);

        // SolutionPlayerに解法をセット
        _solutionPlayer.SetSolution(moveDirections);

        StartMoveExecution();
    }

    private void SubscribeSolutionPlayer()
    {
        // 前回の購読を破棄して新しいCompositeDisposableを作成
        _solutionPlayerDisposables?.Dispose();
        _solutionPlayerDisposables = new CompositeDisposable();

        // SolutionPlayerの状態変化を購読
        _solutionPlayer.PuzzleState
            .Subscribe(state => _searchPuzzleStateSubject.OnNext(state))
            .AddTo(_solutionPlayerDisposables);

        // Undo/Redoボタンの状態をPuzzle状態に連動
        _solutionPlayer.PuzzleState
            .Subscribe(_ => UpdateUndoRedoButtonState())
            .AddTo(_solutionPlayerDisposables);

        _solutionPlayer.CurrentState
            .SelectAwait(async (state, _) => await GetButtonTextAsync(state))
            .SubscribeToText(autoSolveButtonText)
            .AddTo(_solutionPlayerDisposables);

        // ドロップダウンの操作可否を状態に連動（Idle時のみ選択可能）
        _solutionPlayer.CurrentState
            .Subscribe(state => dropdown.interactable = state == SolutionPlayer.State.Idle)
            .AddTo(_solutionPlayerDisposables);
    }

    /// <summary>
    /// 移動実行を開始（新規開始・再開共通）
    /// </summary>
    private void StartMoveExecution()
    {
        // 現在の探索状態を通知（再開時に表示を同期するため）
        _searchPuzzleStateSubject.OnNext(_solutionPlayer.PuzzleState.CurrentValue);

        _autoSolveCts?.Cancel();
        _autoSolveCts?.Dispose();
        _autoSolveCts = new CancellationTokenSource();

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _autoSolveCts.Token,
            this.GetCancellationTokenOnDestroy()
        );

        _solutionPlayer.Play();
        ExecuteMoveSequenceAsync(linkedCts.Token).Forget();
    }

    private async UniTaskVoid ExecuteMoveSequenceAsync(CancellationToken cancellationToken)
    {
        // SolutionPlayerのStepForwardで一手ずつ再生（完了時はSolutionPlayerが自動でCompleted状態に）
        while (_solutionPlayer.CanStepForward && !cancellationToken.IsCancellationRequested)
        {
            _solutionPlayer.StepForward();

            if (executionIntervalSlider.value > 0)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(executionIntervalSlider.value),
                    cancellationToken: cancellationToken
                ).SuppressCancellationThrow();
            }
        }
    }

    public void StopAutoSolve()
    {
        _autoSolveCts?.Cancel();
        _solutionPlayer?.Pause();
    }

    /// <summary>
    /// プレイヤーをリセット（初期状態変更時に呼び出し）
    /// </summary>
    public void ResetPlayer()
    {
        _autoSolveCts?.Cancel();
        _solutionPlayer = null;
    }

    public PuzzleState GetCurrentDisplayState()
    {
        return _solutionPlayer?.PuzzleState.CurrentValue ?? _puzzle.State.CurrentValue;
    }

    public void StartSession()
    {
        _searchPuzzleStateSubject.OnNext(GetCurrentDisplayState());
    }

    /// <summary>
    /// 一時停止（Playing状態のときのみ有効）
    /// </summary>
    public void PauseAutoSolve()
    {
        if (_solutionPlayer?.CurrentState.CurrentValue != PlayerBase.State.Playing) return;

        _autoSolveCts?.Cancel();
        _solutionPlayer.Pause();
    }

    /// <summary>
    /// 再開
    /// </summary>
    private void ResumeAutoSolve()
    {
        if (_solutionPlayer?.CurrentState.CurrentValue != PlayerBase.State.Paused) return;
        StartMoveExecution();
    }

    /// <summary>
    /// 初期状態にリセット
    /// </summary>
    private void ResetToInitialState()
    {
        _solutionPlayer?.Reset();
    }

    private async UniTask<string> GetButtonTextAsync(PlayerBase.State state) => state switch
    {
        PlayerBase.State.Idle => await btnExecuteText.GetLocalizedStringAsync(),
        PlayerBase.State.Playing => await btnPauseText.GetLocalizedStringAsync(),
        PlayerBase.State.Paused => await btnResumeText.GetLocalizedStringAsync(),
        PlayerBase.State.Completed => await btnResetText.GetLocalizedStringAsync(),
        _ => await btnExecuteText.GetLocalizedStringAsync()
    };

    /// <summary>
    /// ゴール状態から初期状態までの経路を復元する
    /// dataMapの親子関係を1回辿るだけで、移動方向リストと状態リストの両方を取得
    /// </summary>
    private (List<Puzzle.MoveDirection> moves, List<PuzzleState> states) ReconstructPath(
        Dictionary<PuzzleState, PuzzleNodeData> dataMap)
    {
        var moves = new List<Puzzle.MoveDirection>();
        var states = new List<PuzzleState> { _goalStateHolder.GoalState.Value };

        PuzzleState? current = _goalStateHolder.GoalState;
        PuzzleState? parent = dataMap.TryGetValue(current.Value, out var data) ? data.Parent : null;

        // ゴール→初期の順で辿りながら、移動方向と状態を同時に収集
        while (parent.HasValue)
        {
            moves.Add(parent.Value.GetMoveDirectionTo(current.Value));
            states.Add(parent.Value);
            current = parent;
            parent = dataMap.TryGetValue(current.Value, out data) ? data.Parent : null;
        }

        // 初期→ゴールの順序に変更
        moves.Reverse();
        states.Reverse();

        return (moves, states);
    }
}