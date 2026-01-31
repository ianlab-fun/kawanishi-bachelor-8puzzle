/// <summary>
/// IVisualizeStrategy インスタンスを生成するプロバイダー
/// SerializeReferenceでInspector上で切り替え可能
/// </summary>
public interface IVisualizeStrategyProvider : IVisualizationStrategyInfo
{
    /// <summary>
    /// 設定に基づいてIVisualizeStrategyインスタンスを生成
    /// </summary>
    IVisualizeStrategy CreateStrategy();
}