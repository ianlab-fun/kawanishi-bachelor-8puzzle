using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// 探索プログレスを表示するUIコンポーネント
/// </summary>
public class SearchProgressDisplay : MonoBehaviour
{
    [SerializeField] private SearchSpaceController searchSpaceController;
    [SerializeField] private LocalizedString progressFormat;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;

    private void Awake()
    {
        // プログレス購読
        searchSpaceController.SearchProgress
            .SubscribeAwait(async (progress, _) =>
            {
                progressText.text = await progressFormat.GetLocalizedStringAsync(progress.ExploredCount, progress.MaxEstimate, progress.Rate);
                progressSlider.value = progress.Rate;
            })
            .AddTo(this);
    }
}