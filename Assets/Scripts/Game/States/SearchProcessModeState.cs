using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using VContainer;

/// <summary>
/// ステップ実行モードの状態クラス
/// </summary>
public sealed class SearchProcessModeState : GameModeStateBase
{
    [SerializeField] private LocalizedString displayName;
    protected override LocalizedString DisplayName => displayName;

    [SerializeField] private GameObject myCanvas;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private LocalizedString guideTextLocalized;

    // DI注入される依存性
    private GameModeInputService _gameModeInputService;

    // 購読管理用
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
    /// ステップ実行モードに入った時の処理
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
    }

    /// <summary>
    /// ステップ実行モードから出る時の処理
    /// </summary>
    protected override void OnExit()
    {
        // 入力購読の解除
        _stateDisposables?.Dispose();
        _stateDisposables = null;

        // 自分のCanvasを非表示にする
        myCanvas.SetActive(false);
    }
}