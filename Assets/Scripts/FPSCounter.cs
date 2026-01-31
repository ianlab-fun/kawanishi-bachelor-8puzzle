using UnityEngine;
using TMPro;

/// <summary>
/// 現在のFPSをTextMeshProUGUIに表示するシンプルなカウンター
/// </summary>
public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f; // 更新間隔（秒）

    private int _frameCount = 0;
    private float _timer = 0f;

    void Update()
    {
        _frameCount++;
        _timer += Time.unscaledDeltaTime;

        // 更新間隔に達したらFPSを計算・表示
        if (_timer >= updateInterval)
        {
            float fps = _frameCount / _timer;
            fpsText.text = $"FPS: {fps:F1}";

            // リセット
            _frameCount = 0;
            _timer = 0f;
        }
    }
}
