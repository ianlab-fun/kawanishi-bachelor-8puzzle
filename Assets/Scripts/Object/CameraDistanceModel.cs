using R3;

/// <summary>
/// カメラ距離のModel
/// </summary>
public class CameraDistanceModel
{
    public ReactiveProperty<float> Distance { get; } = new(15f);
}