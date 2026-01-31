using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 可視化戦略を保持し、ランタイムでの切り替えを管理するクラス
/// IVisualizationStrategyProviderとIVisualizationStrategyControllerの両方を実装し、
/// 読み取り専用アクセスと書き込み用アクセスを分離する
/// </summary>
public class VisualizationStrategyHolder : IVisualizationStrategyProvider, IVisualizationStrategyController
{
    private readonly IVisualizeStrategyProvider[] _providers;
    private readonly IVisualizeStrategy[] _strategies;
    private int _currentIndex;

    /// <summary>
    /// 現在アクティブな可視化戦略を取得します
    /// </summary>
    public IVisualizeStrategy CurrentStrategy => _strategies[_currentIndex];

    /// <summary>
    /// 利用可能なすべての戦略情報を取得します
    /// </summary>
    public IReadOnlyList<IVisualizationStrategyInfo> AllStrategyInfos => _providers;

    /// <summary>
    /// VisualizationStrategyHolderの新しいインスタンスを初期化します
    /// </summary>
    /// <param name="providers">管理する可視化戦略プロバイダーの配列</param>
    public VisualizationStrategyHolder(IVisualizeStrategyProvider[] providers)
    {
        _providers = providers;
        _strategies = providers.Select(p => p.CreateStrategy()).ToArray();
        _currentIndex = 0;
    }

    /// <summary>
    /// 指定されたインデックスの可視化戦略に切り替えます
    /// </summary>
    /// <param name="index">戦略のインデックス（0から始まる）</param>
    public void ChangeStrategy(int index)
    {
        if (index < 0 || index >= _strategies.Length)
        {
            throw new System.ArgumentOutOfRangeException(nameof(index),
                $"Index must be between 0 and {_strategies.Length - 1}");
        }

        _currentIndex = index;
    }
}