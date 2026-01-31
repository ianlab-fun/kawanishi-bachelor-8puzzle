using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Localization;
using VContainer;

/// <summary>
/// 探索モードの状態クラス
/// </summary>
/// <remarks>
/// プレイヤーが3D空間内を自由に移動し、検索アルゴリズムによる状態空間の可視化を観察するモードです。
/// - FPS視点でカメラを自由に動かせます
/// - カーソルはロックされ、非表示になります
/// - 検索アルゴリズム（BFS/DFS/完全探索）の実行と可視化が可能です
/// - パズルの手動操作は無効化されます
/// </remarks>
public sealed class ExplorationModeState : GameModeStateBase
{
    [SerializeField] private SimplePlayerController playerController;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private GameObject myCanvas;
    [SerializeField] private OnScreenStick stick;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private LocalizedString guideTextLocalized;
    [SerializeField] private TextMeshProUGUI returnModeText;
    [SerializeField] private Vector3 initialPosition;

    // DI注入される依存性
    private GameModeInputService _gameModeInputService;

    // 購読管理用（一貫性のため）
    private CompositeDisposable _stateDisposables;

    /// <summary>
    /// 依存性注入
    /// </summary>
    [Inject]
    public void Construct(GameModeInputService gameModeInputService)
    {
        _gameModeInputService = gameModeInputService;
    }

    /// <summary>
    /// 探索モードに入った時の処理
    /// </summary>
    protected override void OnEnter()
    {
        // プレイヤー移動を有効化（3D空間の探索のため）
        playerController.enabled = true;

        // カーソル非表示・ロック（FPS視点のため）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // UIの切り替え
        UniTask.Void(async () => guideText.text = await guideTextLocalized.GetLocalizedStringAsync());
        UniTask.Void(async () => returnModeText.text = PreviousStateDisplayName != null
            ? await PreviousStateDisplayName.GetLocalizedStringAsync()
            : "");
        myCanvas.SetActive(true);
        
        // モバイルスティックを有効化
        stick.enabled = true;
        stick.transform.parent.gameObject.SetActive(true);

        // 購読管理用のCompositeDisposable生成（一貫性のため）
        _stateDisposables = new CompositeDisposable();
        
        Observable.EveryUpdate()
            .Where(_ => Input.GetKeyDown(KeyCode.R))
            .Subscribe(_ => cameraFollow.ForceMoveToPosition(initialPosition))
            .AddTo(_stateDisposables);

        // 右クリックで前のモードに戻る
        _gameModeInputService.GetModeChangeObservable()
            .Subscribe(_ => TransitionToPrevious())
            .AddTo(_stateDisposables);

        // Xキーでゲーム設定画面に戻る
        _gameModeInputService.GetCancelKeyObservable()
            .Subscribe(_ => TransitionTo<GameSetupModeState>())
            .AddTo(_stateDisposables);
    }

    /// <summary>
    /// 探索モードから出る時の処理
    /// </summary>
    protected override void OnExit()
    {
        // 購読の解除
        _stateDisposables?.Dispose();
        _stateDisposables = null;

        // プレイヤー移動を無効化
        playerController.enabled = false;

        // モバイルスティックを無効化
        stick.enabled = false;
        stick.transform.parent.gameObject.SetActive(false);
        
        // 自分のCanvasを非表示にする
        myCanvas.SetActive(false);
    }
}
