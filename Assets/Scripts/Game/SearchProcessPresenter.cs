using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using R3;
using VContainer;

/// <summary>
/// 探索過程可視化モードのPresenter
/// Playerとモデルを調整し、可視化を更新する
/// </summary>
public class SearchProcessPresenter : MonoBehaviour
{
    [SerializeField] private SearchSpaceStateManager stateManager;
    [SerializeField] private SearchSpaceEdgeManager edgeManager;
    [SerializeField] private TMP_Dropdown algorithmDropdown;
    [SerializeField] private Slider executionIntervalSlider;
    [SerializeField] private TextMeshProUGUI executionIntervalText;
    [SerializeField] private LocalizedString executionIntervalFormat;
    [SerializeField] private Button autoPlayButton;
    [SerializeField] private TextMeshProUGUI autoPlayButtonText;
    [SerializeField] private Button stepButton;
    [SerializeField] private Button resetButton;

    [Header("Button Text Localization")]
    [SerializeField] private LocalizedString btnExecuteText;
    [SerializeField] private LocalizedString btnPauseText;
    [SerializeField] private LocalizedString btnResumeText;
    [SerializeField] private LocalizedString btnResetText;

    private Puzzle _initialPuzzle;
    private GoalStateHolder _goalStateHolder;
    private ISearchSpaceEdgeCalculator _edgeCalculator;
    private List<Matrix4x4> _previousStepEdges = new List<Matrix4x4>();

    private SearchProcessPlayer _processPlayer;
    private CompositeDisposable _playerDisposables;
    private CancellationTokenSource _autoPlayCts;

    [Inject]
    public void Construct(Puzzle initialPuzzle, GoalStateHolder goalStateHolder, ISearchSpaceEdgeCalculator edgeCalculator)
    {
        _initialPuzzle = initialPuzzle;
        _goalStateHolder = goalStateHolder;
        _edgeCalculator = edgeCalculator;
    }

    private async void Awake()
    {
        // スライダー値をローカライズ表示（英語: 0.50s、日本語: 0.50秒）
        executionIntervalSlider.onValueChanged.AddListener(UpdateIntervalTextAsync);
        executionIntervalText.text = await executionIntervalFormat.GetLocalizedStringAsync(executionIntervalSlider.value);

        // ボタンテキストの初期値
        autoPlayButtonText.text = await btnExecuteText.GetLocalizedStringAsync();

        autoPlayButton.onClick.AddListener(OnAutoPlayButtonClicked);
        stepButton.onClick.AddListener(ExecuteStep);
        resetButton.onClick.AddListener(ResetSearch);
    }

    private async void UpdateIntervalTextAsync(float value)
    {
        executionIntervalText.text = await executionIntervalFormat.GetLocalizedStringAsync(value);
    }

    /// <summary>
    /// ステップ実行を開始
    /// </summary>
    public void StartSearch()
    {
        // ドロップダウンの値に応じてアルゴリズムを選択
        IStepwiseSearchAlgorithm algorithm = algorithmDropdown.value switch
        {
            0 => new StepwiseBFS(),
            1 => new StepwiseDFS(),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithmDropdown.value))
        };

        // Playerを作成（アルゴリズムの初期化もコンストラクタで行われる）
        _processPlayer = new SearchProcessPlayer(algorithm, _initialPuzzle, _goalStateHolder.GoalState);

        SubscribePlayer();

        // エッジと前回ステップ情報をクリア
        edgeManager.ClearEdges();
        _previousStepEdges.Clear();

        // StateManagerをリセットして初期状態だけを描画
        stateManager.Reset();
        stateManager.AddDiscoveredStates(new[] { _initialPuzzle.State.CurrentValue });
    }

    /// <summary>
    /// 1ステップ実行
    /// </summary>
    public void ExecuteStep()
    {
        if (_processPlayer == null)
        {
            StartSearch();
        }
        _processPlayer.StepForward();
    }

    /// <summary>
    /// ステップ実行結果を受け取って可視化を更新
    /// </summary>
    private void OnStepExecuted(SearchStepResult result)
    {
        // アルゴリズム内部の _puzzleDataMap に自動的に新発見状態が登録されている

        // 展開した状態の隣接状態を増分描画（StateManagerが重複チェックして未表示のみ追加）
        if (result.ExpandedNodeData.AdjacentStates.Any())
        {
            stateManager.AddDiscoveredStates(result.ExpandedNodeData.AdjacentStates);
        }

        // 今回のステップで発見されたエッジを計算
        List<Matrix4x4> currentStepEdges = CalculateStepEdges(result);

        // EdgeManagerで可視化を更新
        // - 前回のrouteEdgeをnormalEdgeに追加
        // - 今回の新エッジをrouteEdge（ハイライト）として表示
        edgeManager.VisualizeStepEdges(_previousStepEdges, currentStepEdges);

        // 新エッジがある場合、fillAmountアニメーション（全てのrouteEdgeが同時に伸びる）
        if (currentStepEdges.Any())
        {
            edgeManager.AnimateRouteEdges();
        }

        // 次回のために今回のエッジを保存
        _previousStepEdges = currentStepEdges;
    }

    /// <summary>
    /// 今回のステップで発見されたエッジを計算
    /// </summary>
    private List<Matrix4x4> CalculateStepEdges(SearchStepResult result)
    {
        List<Matrix4x4> stepEdges = new List<Matrix4x4>();

        // 展開した状態の全ての隣接状態へのエッジを計算（新発見 + 既訪問済み）
        foreach (var adjacentState in result.ExpandedNodeData.AdjacentStates)
        {
            Matrix4x4 edgeMatrix = _edgeCalculator.CalculateEdgeMatrix(result.ExpandedState, adjacentState, edgeManager.NormalEdgeThickness);
            stepEdges.Add(edgeMatrix);
        }

        return stepEdges;
    }

    private void SubscribePlayer()
    {
        _playerDisposables?.Dispose();
        _playerDisposables = new CompositeDisposable();

        // ステップ実行結果を購読
        _processPlayer.OnStepExecuted
            .Subscribe(OnStepExecuted)
            .AddTo(_playerDisposables);

        // ボタンテキストを状態に連動
        _processPlayer.CurrentState
            .SelectAwait(async (state, _) => await GetButtonTextAsync(state))
            .SubscribeToText(autoPlayButtonText)
            .AddTo(_playerDisposables);

        // ドロップダウンの操作可否を状態に連動（Idle時のみ選択可能）
        _processPlayer.CurrentState
            .Subscribe(state => algorithmDropdown.interactable = state == PlayerBase.State.Idle)
            .AddTo(_playerDisposables);
    }

    /// <summary>
    /// 自動再生ボタンクリック時のハンドラ
    /// </summary>
    public void OnAutoPlayButtonClicked()
    {
        Debug.Log("OnAutoPlayButtonClicked");
        if (_processPlayer == null)
        {
            StartSearch();
            StartAutoPlay();
            return;
        }
        Debug.Log(_processPlayer.CurrentState.CurrentValue);

        switch (_processPlayer.CurrentState.CurrentValue)
        {
            case PlayerBase.State.Idle:
                StartSearch();
                StartAutoPlay();
                break;
            case PlayerBase.State.Playing:
                PauseAutoPlay();
                break;
            case PlayerBase.State.Paused:
                ResumeAutoPlay();
                break;
            case PlayerBase.State.Completed:
                ResetSearch();
                break;
        }
    }

    private void StartAutoPlay()
    {
        _autoPlayCts?.Cancel();
        _autoPlayCts?.Dispose();
        _autoPlayCts = new CancellationTokenSource();

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _autoPlayCts.Token,
            this.GetCancellationTokenOnDestroy()
        );

        _processPlayer.Play();
        AutoPlayLoopAsync(linkedCts.Token).Forget();
    }

    private async UniTaskVoid AutoPlayLoopAsync(CancellationToken ct)
    {
        while (_processPlayer.CanStepForward && !ct.IsCancellationRequested)
        {
            _processPlayer.StepForward();

            if (executionIntervalSlider.value > 0)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(executionIntervalSlider.value),
                    cancellationToken: ct
                ).SuppressCancellationThrow();
            }
            else
            {
                // 間隔0でも1フレーム待機してUIをフリーズさせない
                await UniTask.Yield(ct).SuppressCancellationThrow();
            }
        }
    }

    private void PauseAutoPlay()
    {
        if (_processPlayer?.CurrentState.CurrentValue != PlayerBase.State.Playing) return;

        _autoPlayCts?.Cancel();
        _processPlayer.Pause();
    }

    private void ResumeAutoPlay()
    {
        if (_processPlayer?.CurrentState.CurrentValue != PlayerBase.State.Paused) return;
        StartAutoPlay();
    }

    private void ResetSearch()
    {
        // 自動再生を停止（Reset後もCanStepForwardがtrueのため明示的なキャンセルが必要）
        _autoPlayCts?.Cancel();

        _processPlayer?.Reset();

        // 可視化もリセット
        edgeManager.ClearEdges();
        _previousStepEdges.Clear();
        stateManager.Reset();
        stateManager.AddDiscoveredStates(new[] { _initialPuzzle.State.CurrentValue });
    }

    private async UniTask<string> GetButtonTextAsync(PlayerBase.State state) => state switch
    {
        PlayerBase.State.Idle => await btnExecuteText.GetLocalizedStringAsync(),
        PlayerBase.State.Playing => await btnPauseText.GetLocalizedStringAsync(),
        PlayerBase.State.Paused => await btnResumeText.GetLocalizedStringAsync(),
        PlayerBase.State.Completed => await btnResetText.GetLocalizedStringAsync(),
        _ => await btnExecuteText.GetLocalizedStringAsync()
    };
}
