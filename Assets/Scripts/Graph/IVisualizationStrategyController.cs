using System.Collections.Generic;

/// <summary>
/// 可視化戦略の切り替えを制御するインターフェース
/// VisualizationSettingsPresenterなど、戦略を変更するクラスが依存する
/// </summary>
public interface IVisualizationStrategyController
{
    /// <summary>
    /// 利用可能なすべての戦略情報を取得します
    /// </summary>
    IReadOnlyList<IVisualizationStrategyInfo> AllStrategyInfos { get; }

    /// <summary>
    /// 指定されたインデックスの可視化戦略に切り替えます
    /// </summary>
    /// <param name="index">戦略のインデックス（0から始まる）</param>
    void ChangeStrategy(int index);
}