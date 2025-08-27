using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Cinemachine Virtual Cameras")]
    [SerializeField] private CinemachineVirtualCamera _firstPersonCamera;
    [SerializeField] private CinemachineFreeLook _thirdPersonCamera;
    private CinemachineVirtualCameraBase _currentCamera;

    [Header("Target Settings")]
    [SerializeField] private GameObject _headTarget;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float heightOffset = 1.8f; // height above player
    [SerializeField] private float forwardOffset = 0.5f; // forward from player

    void Start()
    {
        // Start in third person by default
        SetActiveCamera(_firstPersonCamera);
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

        // ---- Sync orientation between cameras ----
        if (oldCam != null)
        {
            Vector3 forward = oldCam.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            float targetYaw = Quaternion.LookRotation(forward).eulerAngles.y;

            // If switching to FreeLook, sync its horizontal axis
            if (newCam is CinemachineFreeLook freelook)
            {
                freelook.m_XAxis.Value = targetYaw;
            }
            // If switching to POV (first person), sync its horizontal axis
            else if (newCam is CinemachineVirtualCamera vcam)
            {
                var pov = vcam.GetCinemachineComponent<CinemachinePOV>();
                if (pov != null)
                {
                    pov.m_HorizontalAxis.Value = targetYaw;
                }
            }
        }

        // Raise chosen one
        newCam.Priority = 10;
        _currentCamera = newCam;
    }
}
