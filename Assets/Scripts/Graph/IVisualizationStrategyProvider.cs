/// <summary>
/// 現在の可視化戦略を取得するための読み取り専用インターフェース
/// SearchSpaceControllerなど、戦略を使用するだけで変更しないクラスが依存する
/// </summary>
public interface IVisualizationStrategyProvider
{
    /// <summary>
    /// 現在アクティブな可視化戦略を取得します
    /// </summary>
    IVisualizeStrategy CurrentStrategy { get; }
}
