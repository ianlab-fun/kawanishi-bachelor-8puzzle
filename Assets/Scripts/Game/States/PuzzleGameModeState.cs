using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;
using VContainer;

/// <summary>
/// パズルゲームモードの状態クラス
/// </summary>
/// <remarks>
/// プレイヤーがパズルを手動操作するモードです。
/// - カーソルが表示され、自由に動かせます
/// - パズル操作UI（WASD/矢印キー/マウスクリック/ドラッグ）が有効になります
/// - 3D空間内の移動は無効化されます
/// - Undo/Redoボタンでの操作履歴管理が可能です
///
/// パズル操作ロジックはPuzzleGamePresenterに委譲。
/// このクラスはモード遷移制御のみを担当。
/// </remarks>
public sealed class PuzzleGameModeState : GameModeStateBase
{
    [SerializeField] private LocalizedString displayName;
    protected override LocalizedString DisplayName => displayName;

    [SerializeField] private GameObject myCanvas;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private LocalizedString guideTextLocalized;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private PuzzleView puzzleView;
    [SerializeField] private PuzzleGamePresenter presenter;
    [SerializeField] private PuzzleGameBlockCreator puzzleGameBlockCreator;

    // DI注入
    private Puzzle _puzzle;
    private GameModeInputService _gameModeInputService;
    private PuzzleInputService _puzzleInputService;
    private ISearchSpacePositionReader _positionReader;

    // 購読管理
    private CompositeDisposable _stateDisposables;
    private CompositeDisposable _presenterDisposables;

    [Inject]
    public void Construct(
        Puzzle puzzle,
        GameModeInputService gameModeInputService,
        PuzzleInputService puzzleInputService,
        ISearchSpacePositionReader positionReader)
    {
        _puzzle = puzzle;
        _gameModeInputService = gameModeInputService;
        _puzzleInputService = puzzleInputService;
        _positionReader = positionReader;
    }

    protected override void OnAwake()
    {
        puzzleGameBlockCreator.Initialize(_puzzle.State.CurrentValue);
    }

    /// <summary>
    /// パズルゲームモードに入った時の処理
    /// </summary>
    protected override void OnEnter()
    {
        // カーソル表示・解放
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // UI表示
        UniTask.Void(async () => guideText.text = await guideTextLocalized.GetLocalizedStringAsync());
        myCanvas.SetActive(true);

        // State固有の購読（StartSession()より先に設定）
        _stateDisposables = new CompositeDisposable();

        // カメラ追従購読（StartSession()より先に設定）
        presenter.GamePuzzleState
            .Subscribe(state =>
            {
                Vector3 position = _positionReader.StatePositions[state];
                cameraFollow.FollowToPosition(position);
            })
            .AddTo(_stateDisposables);

        // パズル状態変更のアニメーション購読（StartSession()より先に設定）
        presenter.GamePuzzleState
            .Pairwise()
            .Where(x => PuzzleGamePresenter.IsUserOperationChange(x.Previous, x.Current))
            .Subscribe(x => puzzleView.AnimateMove(x.Current, x.Previous))
            .AddTo(_stateDisposables);

        // Presenterセッション開始（購読設定後に呼ぶ）
        _presenterDisposables = presenter.StartSession(_puzzleInputService);

        // 右クリックでExplorationModeに遷移
        _gameModeInputService.GetModeChangeObservable()
            .Subscribe(_ => TransitionTo<ExplorationModeState>())
            .AddTo(_stateDisposables);

        // Xキーでゲーム設定画面に戻る
        _gameModeInputService.GetCancelKeyObservable()
            .Subscribe(_ => TransitionTo<GameSetupModeState>())
            .AddTo(_stateDisposables);
    }

    /// <summary>
    /// パズルゲームモードから出る時の処理
    /// </summary>
    protected override void OnExit()
    {
        // Presenterの購読を解除
        _presenterDisposables?.Dispose();
        _presenterDisposables = null;

        // State固有の購読を解除
        _stateDisposables?.Dispose();
        _stateDisposables = null;

        // Canvasを非表示
        myCanvas.SetActive(false);
    }
}
