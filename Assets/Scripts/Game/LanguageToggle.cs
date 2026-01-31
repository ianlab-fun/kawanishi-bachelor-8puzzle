using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 言語切り替えトグル
/// </summary>
public class LanguageToggle : MonoBehaviour
{
    private const string PrefsKey = "Locale";
    private const string DefaultLocale = "ja-JP";

    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private VisualizationStrategySelect visualizationStrategySelect;

    private async void Awake()
    {
        // PlayerPrefsから復元（なければデフォルト）
        var code = PlayerPrefs.GetString(PrefsKey, DefaultLocale);
        await LocalizationSettings.InitializationOperation;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        LocalizationSettings.SelectedLocale = locale;

        button.onClick.AddListener(Toggle);

        var culture = GetNextLocale().Identifier.CultureInfo;
        var parent = culture?.Parent;
        label.text = string.IsNullOrEmpty(parent?.Name) ? culture?.NativeName : parent.NativeName;

        await visualizationStrategySelect.InitializeDropdownAsync();
    }

    private void Toggle()
    {
        var next = GetNextLocale();
        LocalizationSettings.SelectedLocale = next;
        PlayerPrefs.SetString(PrefsKey, next.Identifier.Code);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    private Locale GetNextLocale()
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        var currentIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);
        return locales[(currentIndex + 1) % locales.Count];
    }
}