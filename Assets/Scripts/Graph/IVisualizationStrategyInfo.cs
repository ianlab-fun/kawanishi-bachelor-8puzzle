using UnityEngine.Localization;

/// <summary>
/// 可視化戦略のメタ情報を提供するインターフェース
/// UI表示用の読み取り専用情報
/// </summary>
public interface IVisualizationStrategyInfo
{
    /// <summary>
    /// ドロップダウンに表示する名前
    /// </summary>
    LocalizedString DisplayName { get; }

    /// <summary>
    /// 可視化戦略の説明テキスト
    /// </summary>
    LocalizedString Description { get; }
}