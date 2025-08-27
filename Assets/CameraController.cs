using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Cinemachine Virtual Cameras")]
    [SerializeField] private CinemachineVirtualCamera _firstPersonCamera;
    [SerializeField] private CinemachineFreeLook _thirdPersonCamera;
    private CinemachineVirtualCameraBase _currentCamera;

    [Header("Camera Controllers")]
    [SerializeField] private FirstPersonCameraController _firstPersonCameraController;

    [Header("Target Settings")]
    [SerializeField] private GameObject _headTarget;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float heightOffset = 1.8f; // height above player
    [SerializeField] private float forwardOffset = 0.5f; // forward from player

    void Start()
    {
        SetActiveCamera(_thirdPersonCamera);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (_currentCamera == _firstPersonCamera)
                SetActiveCamera(_thirdPersonCamera, _firstPersonCamera);
            else
                SetActiveCamera(_firstPersonCamera, _thirdPersonCamera);
        }
    }

    void LateUpdate()
    {
        if (_playerTransform == null || _headTarget == null || _currentCamera == null) return;

        // Base position: above the player
        Vector3 basePosition = _playerTransform.position + Vector3.up * heightOffset;

        // Forward offset based on current camera forward
        Vector3 cameraForward = _currentCamera.transform.forward;
        cameraForward.y = 0f; // ignore vertical tilt
        cameraForward.Normalize();

        Vector3 targetPosition = basePosition + cameraForward * forwardOffset;
        _headTarget.transform.position = targetPosition;
    }

    private void SetActiveCamera(CinemachineVirtualCameraBase newCam, CinemachineVirtualCameraBase oldCam = null)
    {
        // Reset priorities
        _thirdPersonCamera.Priority = 0;
        _firstPersonCamera.Priority = 0;

        // Third -> First
        if (newCam == _firstPersonCamera && oldCam == _thirdPersonCamera)
        {
            // Align first-person camera to the player's facing direction
            _firstPersonCamera.transform.rotation = Quaternion.LookRotation(_playerTransform.forward, Vector3.up);

            // Optional: also tell your FirstPersonCameraController the initial yaw
            if (_firstPersonCameraController != null)
            {
                _firstPersonCameraController.SetYawFromPlayer(_playerTransform.eulerAngles.y);
            }
        }

        // First -> Third
        if (newCam == _thirdPersonCamera && oldCam == _firstPersonCamera)
        {
            // Sync to player’s actual facing direction
            Vector3 forward = _playerTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            float targetYaw = Quaternion.LookRotation(forward).eulerAngles.y;

            _thirdPersonCamera.m_XAxis.Value = targetYaw;
            _thirdPersonCamera.m_YAxis.Value = 0.5f;
        }

        // Raise chosen one
        newCam.Priority = 10;
        _currentCamera = newCam;

        // Enable/disable first-person controller
        if (_firstPersonCameraController != null)
            _firstPersonCameraController.enabled = (newCam == _firstPersonCamera);
    }
}
