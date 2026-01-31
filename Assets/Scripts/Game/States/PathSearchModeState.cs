using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;
using VContainer;

/// <summary>
/// 自動探索モードの状態クラス
/// </summary>
public sealed class PathSearchModeState : GameModeStateBase
{
    [SerializeField] private LocalizedString displayName;
    protected override LocalizedString DisplayName => displayName;

    [SerializeField] private GameObject myCanvas;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private PathSearchPresenter pathSearchPresenter;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private LocalizedString guideTextLocalized;
    [SerializeField] private PuzzleView puzzleView;
    [SerializeField] private PuzzleGameBlockCreator puzzleGameBlockCreator;

    // DI注入される依存性
    private GameModeInputService _gameModeInputService;

    // 購読管理用
    private CompositeDisposable _stateDisposables;
    private Puzzle _puzzle;
    private ISearchSpacePositionReader _positionReader;

    /// <summary>
    /// 依存性注入
    /// </summary>
    [Inject]
    public void Construct(
        Puzzle puzzle,
        GameModeInputService gameModeInputService,
        ISearchSpacePositionReader searchSpacePositionReader)
    {
        _puzzle = puzzle;
        _gameModeInputService = gameModeInputService;
        _positionReader = searchSpacePositionReader;
    }

    protected override void OnAwake()
    {
        // PuzzleGameBlockCreatorを初期化（ゲーム起動時に1回だけ）
        puzzleGameBlockCreator.Initialize(_puzzle.State.CurrentValue);
    }

    /// <summary>
    /// 自動探索モードに入った時の処理
    /// </summary>
    protected override void OnEnter()
    {
        // カーソル表示・解放（UI操作のため）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // UIの切り替え
        UniTask.Void(async () => guideText.text = await guideTextLocalized.GetLocalizedStringAsync());
        myCanvas.SetActive(true);

        // 入力Observableの購読開始
        _stateDisposables = new CompositeDisposable();

        // Xキーでゲーム設定画面に戻る
        _gameModeInputService.GetCancelKeyObservable()
            .Subscribe(_ => TransitionTo<GameSetupModeState>())
            .AddTo(_stateDisposables);
        
        // 右クリックでExplorationModeに遷移
        _gameModeInputService.GetModeChangeObservable()
            .Subscribe(_ => TransitionTo<ExplorationModeState>())
            .AddTo(_stateDisposables);
        
        // カメラ追従の購読
        pathSearchPresenter.SearchPuzzleState
            .Subscribe(x =>
            {
                Vector3 position = _positionReader.StatePositions[x];
                cameraFollow.FollowToPosition(position);
            })
            .AddTo(_stateDisposables);

        // パズル状態変更のアニメーション購読
        pathSearchPresenter.SearchPuzzleState
            .Prepend(pathSearchPresenter.GetCurrentDisplayState())
            .Pairwise()
            .Subscribe(x => puzzleView.AnimateMove(x.Current, x.Previous))
            .AddTo(_stateDisposables);

        // 購読設定後にセッション開始（UIを現在状態に同期）
        pathSearchPresenter.StartSession();
    }

    /// <summary>
    /// 自動探索モードから出る時の処理
    /// </summary>
    protected override void OnExit()
    {
        // モード遷移時は一時停止（戻ってきたら再開できる）
        pathSearchPresenter.PauseAutoSolve();

        // 入力購読の解除
        _stateDisposables?.Dispose();
        _stateDisposables = null;

        // 自分のCanvasを非表示にする
        myCanvas.SetActive(false);
    }
}