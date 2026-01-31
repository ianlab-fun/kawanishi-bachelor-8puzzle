using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using VContainer;

/// <summary>
/// 可視化戦略選択UIコンポーネント
/// ドロップダウンUIと戦略変更ロジックを管理する
/// </summary>
public class VisualizationStrategySelect : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown visualizationDropdown;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private SearchSpaceController searchSpaceController;
    [SerializeField] private GameObject touchGuard;

    private IVisualizationStrategyController _strategyController;

    // 現在選択中の戦略インデックス
    private int _currentStrategyIndex = 0;

    // 前回実行時の状態（初回はnullで未実行を表現）
    private PuzzleState? _lastExecutedStartState = null;
    private int? _lastExecutedStrategyIndex = null;

    [Inject]
    public void Construct(IVisualizationStrategyController strategyController)
    {
        _strategyController = strategyController;
    }


    /// <summary>
    /// 探索・可視化を無条件で実行（判定は呼び出し側で行う）
    /// </summary>
    public async UniTask ExecuteSearchAsync(PuzzleState startState, CancellationToken cancellationToken = default)
    {
        touchGuard.SetActive(true);

#if UNITY_WEBGL
        searchSpaceController.ExecuteSearch();
        searchSpaceController.UpdateVisualization();
#else
        await searchSpaceController.ExecuteSearchAsync(cancellationToken);
        await searchSpaceController.UpdateVisualizationAsync(cancellationToken);
#endif

        touchGuard.SetActive(false);
        UpdateLastExecutedState(startState);
    }

    /// <summary>
    /// ドロップダウンを初期化し、利用可能な戦略のリストを設定する
    /// </summary>
    public async UniTask InitializeDropdownAsync()
    {
        var options = new System.Collections.Generic.List<string>();
        foreach (var info in _strategyController.AllStrategyInfos)
        {
            options.Add(await info.DisplayName.GetLocalizedStringAsync());
        }

        visualizationDropdown.ClearOptions();
        visualizationDropdown.AddOptions(options);
        visualizationDropdown.onValueChanged.AddListener(OnVisualizationChangedAsync);

        // 初期選択の説明文を設定
        _strategyController.ChangeStrategy(visualizationDropdown.value);
        _currentStrategyIndex = visualizationDropdown.value;
        descriptionText.text = await _strategyController.AllStrategyInfos[visualizationDropdown.value].Description.GetLocalizedStringAsync();
    }

    /// <summary>
    /// ドロップダウンの選択が変更されたときの処理
    /// </summary>
    /// <param name="index">選択された戦略のインデックス</param>
    private async void OnVisualizationChangedAsync(int index)
    {
        _strategyController.ChangeStrategy(index);
        _currentStrategyIndex = index;
        descriptionText.text = await _strategyController.AllStrategyInfos[index].Description.GetLocalizedStringAsync();
    }

    /// <summary>
    /// 前回実行時から状態または戦略が変更されたかを判定します
    /// 初回実行時（前回の記録がない場合）は常にtrueを返します
    /// </summary>
    /// <param name="currentStartState">現在のスタート状態</param>
    /// <returns>変更があった場合true</returns>
    public bool HasChangedSinceLastExecution(PuzzleState currentStartState)
    {
        // 初回実行の場合
        if (!_lastExecutedStartState.HasValue || !_lastExecutedStrategyIndex.HasValue)
        {
            return true;
        }

        // スタート状態の変更をチェック（PuzzleState.Equalsを使用）
        bool stateChanged = !currentStartState.Equals(_lastExecutedStartState.Value);

        // 可視化戦略の変更をチェック
        bool strategyChanged = _currentStrategyIndex != _lastExecutedStrategyIndex.Value;

        return stateChanged || strategyChanged;
    }

    /// <summary>
    /// 現在の状態を記録します（実行後に呼び出す）
    /// </summary>
    /// <param name="startState">記録するスタート状態</param>
    private void UpdateLastExecutedState(PuzzleState startState)
    {
        _lastExecutedStartState = startState;
        _lastExecutedStrategyIndex = _currentStrategyIndex;
    }

    private void OnDestroy()
    {
        visualizationDropdown.onValueChanged.RemoveListener(OnVisualizationChangedAsync);
    }
}
