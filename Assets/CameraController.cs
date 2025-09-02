using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Cinemachine Virtual Cameras")]
    [SerializeField] private CinemachineVirtualCamera _firstPersonCamera;
    [SerializeField] private CinemachineFreeLook _thirdPersonCamera;
    [SerializeField] private CinemachineFreeLook _closeUpCamera;
    private CinemachineVirtualCameraBase _currentCamera;

    [Header("Camera Controllers")]
    [SerializeField] private FirstPersonCameraController _firstPersonCameraController;

    [Header("Target Settings")]
    [SerializeField] private Transform _playerTransform;

    private CinemachineVirtualCameraBase[] _cameras;
    private int _cameraIndex = 0;

    void Start()
    {
        // Put cameras into an array for cycling
        _cameras = new CinemachineVirtualCameraBase[]
        {
            _thirdPersonCamera,
            _firstPersonCamera,
            _closeUpCamera
        };

        // Start with third person
        _cameraIndex = 0;
        SetActiveCamera(_cameras[_cameraIndex]);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Cycle index
            _cameraIndex = (_cameraIndex + 1) % _cameras.Length;

            // Set new camera
            SetActiveCamera(_cameras[_cameraIndex], _currentCamera);
        }
    }

    private void SetActiveCamera(CinemachineVirtualCameraBase newCam, CinemachineVirtualCameraBase oldCam = null)
    {
        // Reset priorities
        _thirdPersonCamera.Priority = 0;
        _firstPersonCamera.Priority = 0;
        _closeUpCamera.Priority = 0;

        // Third -> First
        if (newCam == _firstPersonCamera && oldCam == _thirdPersonCamera)
        {
            _firstPersonCamera.transform.rotation = Quaternion.LookRotation(_playerTransform.forward, Vector3.up);

            if (_firstPersonCameraController != null)
                _firstPersonCameraController.SetYawFromPlayer(_playerTransform.eulerAngles.y);
        }

        // First -> Third
        if (newCam == _thirdPersonCamera && oldCam == _firstPersonCamera)
        {
            Vector3 forward = _playerTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            float targetYaw = Quaternion.LookRotation(forward).eulerAngles.y;
            _thirdPersonCamera.m_XAxis.Value = targetYaw;
            _thirdPersonCamera.m_YAxis.Value = 0.5f;
        }

        newCam.Priority = 10;
        _currentCamera = newCam;

        if (_firstPersonCameraController != null)
            _firstPersonCameraController.enabled = (newCam == _firstPersonCamera);
    }
}
