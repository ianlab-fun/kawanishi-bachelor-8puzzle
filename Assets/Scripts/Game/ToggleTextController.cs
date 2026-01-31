using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// Toggleの状態に応じてTextを切り替える汎用コンポーネント
/// </summary>
/// <remarks>
/// ToggleのON/OFFに応じてテキストを自動的に切り替えます。
/// R3のリアクティブプログラミングを使用して、Toggleの変更を購読します。
/// </remarks>
public class ToggleTextController : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private LocalizedString textWhenOn;
    [SerializeField] private LocalizedString textWhenOff;

    private void Awake()
    {
        // ToggleのisOnの変更を購読してテキストを更新
        toggle.OnValueChangedAsObservable()
            .Prepend(toggle.isOn) // 初期値も流す
            .SubscribeAwait(async (isOn, _) => targetText.text = isOn
                ? await textWhenOn.GetLocalizedStringAsync()
                : await textWhenOff.GetLocalizedStringAsync())
            .AddTo(this);
    }
}