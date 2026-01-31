using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// ゲーム設定モードの状態クラス
/// </summary>
/// <remarks>
/// 探索設定とモード選択を統合した初期画面です。
/// ゲーム開始時の初期状態として使用されます。
/// - カーソルが表示され、自由に動かせます
/// - 探索設定UI（アルゴリズム選択ドロップダウンなど）が表示されます
/// - モード選択ボタン（ステップ実行/パズル追従/自動探索）が表示されます
/// - 3D空間内の移動は無効化されます
/// - 各モードボタンをクリックすると、探索を実行してから対応するモードに遷移します
/// </remarks>
public sealed class GameSetupModeState : GameModeStateBase
{
    [SerializeField] private GameObject myCanvas;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private LocalizedString guideTextLocalized;
    [SerializeField] private VisualizationStrategySelect visualizationStrategySelect;

    [SerializeField] private PuzzleView puzzleView;
    [SerializeField] private PuzzleFinderView finderView;
    [SerializeField] private PuzzleGameBlockCreator puzzleGameBlockCreator;
    
    [SerializeField] private Button stepwiseExecutionButton;
    [SerializeField] private Button puzzleFollowButton;
    [SerializeField] private Button autoSearchButton;
    [SerializeField] private PathSearchPresenter pathSearchPresenter;
    [SerializeField] private PuzzleGamePresenter puzzleGamePresenter;
    [SerializeField] private PuzzleGoalStatePresenter puzzleGoalStatePresenter;

    // 購読管理用
    private CompositeDisposable _stateDisposables;
    private Puzzle _puzzle;
    private GoalStateHolder _goalStateHolder;

    [Inject]
    public void Construct(Puzzle puzzle, GoalStateHolder goalStateHolder)
    {
        _puzzle = puzzle;
        _goalStateHolder = goalStateHolder;
    }

    /// <summary>
    /// 初期化処理（初期状態として自分で OnEnter を呼ぶ）
    /// </summary>
    protected override void OnAwake()
    {
        // 初期状態なので自分で遷移させる
        TransitionTo<GameSetupModeState>();
        // PuzzleGameBlockCreatorを初期化（ゲーム起動時に1回だけ）
        puzzleGameBlockCreator.Initialize(_puzzle.State.CurrentValue);
    }

    /// <summary>
    /// ゲーム設定モードに入った時の処理
    /// </summary>
    protected override void OnEnter()
    {
        // カーソル表示・解放（設定UIの操作のため）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // UIの切り替え（言語設定の初期化完了を待ってからテキスト取得）
        UniTask.Void(async () =>
        {
            await LocalizationSettings.InitializationOperation;
            // PlayerPrefsから言語を復元して再取得
            var code = PlayerPrefs.GetString("Locale", "ja-JP");
            var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale != null)
                LocalizationSettings.SelectedLocale = locale;
            guideText.text = await guideTextLocalized.GetLocalizedStringAsync();
        });
        myCanvas.SetActive(true);

        // 入力Observableの購読開始
        _stateDisposables = new CompositeDisposable();
        
        // finderView.AppliedStateの購読とView更新
        finderView.AppliedState
            .Where(state => state.HasValue)
            .Select(state => state.Value)
            .Subscribe(state =>
            {
                _puzzle.SetPuzzle(state);
                puzzleView.SetPositionImmediate(state);
            })
            .AddTo(_stateDisposables);

        // 各モードボタンで共通遷移処理を使用
        stepwiseExecutionButton.OnClickAsObservable()
            .SubscribeAwait(async (_, ct) => await TransitionToModeAsync<SearchProcessModeState>(ct))
            .AddTo(_stateDisposables);

        puzzleFollowButton.OnClickAsObservable()
            .SubscribeAwait(async (_, ct) => await TransitionToModeAsync<PuzzleGameModeState>(ct))
            .AddTo(_stateDisposables);

        autoSearchButton.OnClickAsObservable()
            .SubscribeAwait(async (_, ct) => await TransitionToModeAsync<PathSearchModeState>(ct))
            .AddTo(_stateDisposables);
    }

    /// <summary>
    /// 共通のモード遷移処理
    /// 初期状態や戦略が変更されていた場合のみ探索を実行
    /// </summary>
    private async UniTask TransitionToModeAsync<T>(CancellationToken ct) where T : GameModeStateBase
    {
        var startState = _puzzle.State.CurrentValue;

        // パリティ更新
        _goalStateHolder.UpdateParity(startState);

        // 変更があった場合のみ実行
        if (visualizationStrategySelect.HasChangedSinceLastExecution(startState))
        {
            // 関連プレゼンターをリセット
            pathSearchPresenter.ResetPlayer();
            puzzleGamePresenter.ResetGamePuzzle();

            // 目標状態のUIを更新
            puzzleGoalStatePresenter.UpdateView();

            // 探索・可視化を実行
            await visualizationStrategySelect.ExecuteSearchAsync(startState, ct);
        }

        TransitionTo<T>();
    }
    
    /// <summary>
    /// ゴール状態をFinderビューに反映
    /// </summary>
    public void StateToFinder()
    {
        finderView.SetPuzzle(_puzzle.State.CurrentValue);
    }

    /// <summary>
    /// ゲーム設定モードから出る時の処理
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
