using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// スペクテイターモードのプレイヤーコントローラー
/// カメラの向いている方向基準で3D空間を自由移動（重力なし）
/// </summary>
public class SimplePlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float lookSensitivity = 5f;
    [SerializeField] private InputActionAsset playerActions;

    public Camera mainCamera;

    private Vector3 _euler;
    private Vector3 _cameraPosition;

    private void Start() => _cameraPosition = transform.position;

    private void OnEnable()
    {
        playerActions["Move"].Enable();
        playerActions["Look"].Enable();
    }

    private void OnDisable()
    {
        playerActions["Move"].Disable();
        playerActions["Look"].Disable();
    }

    private void Update()
    {
        Look(playerActions["Look"].ReadValue<Vector2>());
        Move(playerActions["Move"].ReadValue<Vector2>());
    }

    private void Look(Vector2 input)
    {
        _euler.x -= input.y * lookSensitivity * Time.deltaTime;
        _euler.y += input.x * lookSensitivity * Time.deltaTime;
        _euler.x = Mathf.Clamp(_euler.x, -89.99f, 89.99f);
        mainCamera.transform.rotation = Quaternion.Euler(_euler);
    }

    private void Move(Vector2 input)
    {
        var movement = mainCamera.transform.forward * input.y + mainCamera.transform.right * input.x;
        transform.position += movement * (moveSpeed * Time.deltaTime);

        _cameraPosition = Vector3.Lerp(_cameraPosition, transform.position, Time.deltaTime * 20f);
        mainCamera.transform.position = _cameraPosition;
    }

    /// <summary>
    /// カメラ位置を即座に設定（CameraFollow連携用）
    /// </summary>
    public void SetPositionImmediate(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        _cameraPosition = targetPosition;
    }
}