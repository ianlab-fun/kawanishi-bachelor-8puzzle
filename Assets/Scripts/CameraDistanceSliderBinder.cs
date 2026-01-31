using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// スライダーとCameraDistanceModelを双方向バインド
/// </summary>
public class CameraDistanceSliderBinder : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI distanceText;

    private CameraDistanceModel _model;

    [Inject]
    public void Construct(CameraDistanceModel model) => _model = model;

    private void Start()
    {
        // Model → Slider
        _model.Distance.Subscribe(d =>
        {
            slider.SetValueWithoutNotify(d);
            distanceText.text = $"{d:F1}";
        }).AddTo(this);

        // Slider → Model
        slider.onValueChanged.AddListener(v => _model.Distance.Value = v);
    }
}