using LitMotion;
using R3;
using UnityEngine;
using VContainer;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private SimplePlayerController playerController;
    [SerializeField] private float tweenDuration = 0.2f;

    private CameraDistanceModel _distanceModel;
    private MotionHandle _tweenHandle;
    private Vector3 _lastTargetPosition;

    [Inject]
    public void Construct(CameraDistanceModel distanceModel) => _distanceModel = distanceModel;

    private void Start()
    {
        // カメラ距離変更時にカメラ位置を更新
        _distanceModel.Distance.Subscribe(dist =>
        {
            playerController.mainCamera.transform.position = _lastTargetPosition + Vector3.back * dist;
        }).AddTo(this);
    }

    public void FollowToPosition(Vector3 targetPosition)
    {
        _lastTargetPosition = targetPosition;
        // カメラの最終位置（距離込み）を設定
        playerController.SetPositionImmediate(targetPosition + Vector3.back * _distanceModel.Distance.CurrentValue);
        MoveToPosition(targetPosition);
    }

    public void MoveToPosition(Vector3 targetPosition)
    {
        playerController.mainCamera.transform.rotation = Quaternion.LookRotation(Vector3.forward);

        _tweenHandle.TryCancel();
        _tweenHandle = LMotion.Create(playerController.mainCamera.transform.position, targetPosition + Vector3.back * _distanceModel.Distance.CurrentValue, tweenDuration)
            .WithEase(Ease.OutQuad)
            .Bind(pos => playerController.mainCamera.transform.position = pos)
            .AddTo(this);
    }

    // 探索モード用: playerControllerを一時無効化してカメラと同時に移動
    public void ForceMoveToPosition(Vector3 targetPosition)
    {
        playerController.enabled = false;

        var startControllerPos = playerController.transform.position;
        var startCameraPos = playerController.mainCamera.transform.position;
        var endCameraPos = targetPosition + Vector3.back * _distanceModel.Distance.CurrentValue;

        _tweenHandle.TryCancel();
        _tweenHandle = LMotion.Create(0f, 1f, tweenDuration)
            .WithEase(Ease.OutQuad)
            .WithOnComplete(() => playerController.enabled = true)
            .Bind(t =>
            {
                playerController.transform.position = Vector3.Lerp(startControllerPos, targetPosition, t);
                playerController.mainCamera.transform.position = Vector3.Lerp(startCameraPos, endCameraPos, t);
            })
            .AddTo(this);
    }
}