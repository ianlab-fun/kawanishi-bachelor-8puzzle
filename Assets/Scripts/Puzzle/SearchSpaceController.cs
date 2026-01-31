using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

/// <summary>
/// 検索空間全体の初期化と調整を担当するコントローラークラス
/// StateManagerとEdgeManagerを統括し、検索と可視化のワークフローを管理する
/// 状態配置はSearchSpacePositionServiceに委譲
/// </summary>
public class SearchSpaceController : MonoBehaviour
{
    [SerializeField] private SearchSpaceStateManager stateManager;
    [SerializeField] private SearchSpaceEdgeManager edgeManager;
    [SerializeField] private PathSearchPresenter pathSearchPresenter;

    // 依存性注入されるフィールド
    private Puzzle _puzzle;
    private IVisualizationStrategyProvider _strategyProvider;
    private ISearchAlgorithm _searchAlgorithm;
    private ISearchSpacePositionWriter _positionWriter;

    // プログレス報告用
    private readonly Subject<SearchProgress> _searchProgressSubject = new();
    private Observable<SearchProgress> _searchProgressOnMainThread;
    public Observable<SearchProgress> SearchProgress => _searchProgressOnMainThread ??= _searchProgressSubject.ObserveOnMainThread();

    private CancellationTokenSource _searchCts;

    [Inject]
    public void Construct(Puzzle puzzle, IVisualizationStrategyProvider strategyProvider, ISearchAlgorithm searchAlgorithm, ISearchSpacePositionWriter positionWriter)
    {
        _puzzle = puzzle;
        _strategyProvider = strategyProvider;
        _searchAlgorithm = searchAlgorithm;
        _positionWriter = positionWriter;
    }

    /// <summary>
    /// 検索を実行して可視化します（同期版）
    /// </summary>
    public void ExecuteSearch()
    {
        Debug.Log("Initializing SearchSpaceController");

        // 検索実行
        _searchAlgorithm.Search(_puzzle, null);
    }

    /// <summary>
    /// 検索を実行して可視化します（非同期版）
    /// </summary>
    public async UniTask ExecuteSearchAsync(CancellationToken externalToken = default)
    {
        Debug.Log("Initializing SearchSpaceController (Async)");

        // 前回の探索をキャンセル
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = CancellationTokenSource.CreateLinkedTokenSource(
            externalToken,
            this.GetCancellationTokenOnDestroy()
        );

        // SearchProgressインスタンスを生成
        var progress = new SearchProgress(p => _searchProgressSubject.OnNext(p));

        await _searchAlgorithm.SearchAsync(
            _puzzle,
            null, // 全状態空間を探索
            progress,
            _searchCts.Token
        );

        // 探索完了を通知
        progress.Complete();
    }

    private void OnDestroy()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchProgressSubject.Dispose();
    }

    /// <summary>
    /// 現在の可視化戦略で表示を更新します
    /// 戦略切り替え時やアルゴリズム再実行時に呼び出されます
    /// </summary>
    public void UpdateVisualization()
    {
        // 探索空間が再構築されるため、自動探索をリセット
        pathSearchPresenter.StopAutoSolve();

        var nodeData = _searchAlgorithm.GetResult();

        // 検索結果のログ出力
        LogSearchResult(nodeData);

        // 0. 既存の表示をクリア（戦略切り替え時に古いオブジェクトが残らないようにする）
        edgeManager.ClearEdges();
        stateManager.Reset();

        // 現在の戦略で可視化を実行
        var strategy = _strategyProvider.CurrentStrategy;
        var statePositions = strategy.VisualizeSearchSpace(nodeData, _puzzle.State.CurrentValue);

        // PositionServiceに状態位置を設定（Single Source of Truth）
        _positionWriter.SetStatePositions(statePositions);

        // 1. EdgeManagerの初期化（先に初期化してedgeThicknessを設定）
        edgeManager.Initialize(nodeData, _puzzle);

        // 2. StateManagerの初期化（edgeThicknessを渡してパズルブロックの厚みを自動調整）
        stateManager.Initialize(edgeManager.EdgeThickness);

        // 3. デバッグ情報の出力
        // LogDepthStatistics(nodeData);
    }

    /// <summary>
    /// 現在の可視化戦略で表示を更新します（非同期版）
    /// </summary>
    public async UniTask UpdateVisualizationAsync(CancellationToken cancellationToken = default)
    {
        // 探索空間が再構築されるため、自動探索をリセット
        pathSearchPresenter.StopAutoSolve();

        var nodeData = _searchAlgorithm.GetResult();

        // 検索結果のログ出力
        LogSearchResult(nodeData);

        // 0. 既存の表示をクリア
        edgeManager.ClearEdges();
        stateManager.Reset();

        // SearchProgressインスタンスを生成
        var progress = new SearchProgress(p => _searchProgressSubject.OnNext(p));

        // 現在の戦略で可視化を実行（非同期）
        var strategy = _strategyProvider.CurrentStrategy;
        var statePositions = await strategy.VisualizeSearchSpaceAsync(
            nodeData,
            _puzzle.State.CurrentValue,
            progress,
            cancellationToken
        );

        // 可視化完了を通知
        progress.Complete();

        // PositionServiceに状態位置を設定（Single Source of Truth）
        _positionWriter.SetStatePositions(statePositions);

        // 1. EdgeManagerの初期化
        edgeManager.Initialize(nodeData, _puzzle);

        // 2. StateManagerの初期化
        stateManager.Initialize(edgeManager.EdgeThickness);
    }


    /// <summary>
    /// 検索結果をログ出力する（デバッグ用）
    /// </summary>
    private void LogSearchResult(Dictionary<PuzzleState, PuzzleNodeData> nodeData)
    {
        if (nodeData.Any())
        {
            Debug.Log($"探索完了。総状態数: {nodeData.Count}");
        }
        else
        {
            Debug.Log("ゴールに到達できませんでした");
        }
    }

    /// <summary>
    /// 深さごとの状態数をログ出力する（デバッグ用）
    /// </summary>
    private void LogDepthStatistics(Dictionary<PuzzleState, PuzzleNodeData> nodeData)
    {
        for (int depth = 0; depth <= 31; depth++)
        {
            int count = nodeData.Count(kvp => kvp.Value.Depth == depth);
            if (count > 0)
            {
                Debug.Log($"Depth {depth}: {count} states");
            }
        }
    }
}
