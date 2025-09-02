using UnityEngine;
using Cinemachine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Cinemachine Virtual Cameras")]
    [SerializeField] private CinemachineVirtualCamera _firstPersonCamera;
    [SerializeField] private CinemachineFreeLook _thirdPersonCamera;
    [SerializeField] private CinemachineFreeLook _closeUpCamera;
    [SerializeField] private CinemachineVirtualCamera _interactCamera;

    private CinemachineVirtualCameraBase _currentCamera;
    private CinemachineVirtualCameraBase[] _cameras;
    private int _cameraIndex = 0;

    [Header("Camera Controllers")]
    [SerializeField] private FirstPersonCameraController _firstPersonCameraController;

    [Header("Target Settings")]
    [SerializeField] private Transform _playerTransform;

    private Coroutine _interactRoutine;

    void Start()
    {
        _cameras = new CinemachineVirtualCameraBase[]
        {
            _thirdPersonCamera,
            _firstPersonCamera,
            _closeUpCamera
        };

        _cameraIndex = 0;
        SetActiveCamera(_cameras[_cameraIndex]);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            _cameraIndex = (_cameraIndex + 1) % _cameras.Length;
            SetActiveCamera(_cameras[_cameraIndex], _currentCamera);
        }
    }

    private void SetActiveCamera(CinemachineVirtualCameraBase newCam, CinemachineVirtualCameraBase oldCam = null)
    {
        _thirdPersonCamera.Priority = 0;
        _firstPersonCamera.Priority = 0;
        _closeUpCamera.Priority = 0;
        _interactCamera.Priority = 0;

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

    public void InteractCutScene(GameObject npc)
    {
        if (_interactRoutine != null)
            StopCoroutine(_interactRoutine);

        _interactRoutine = StartCoroutine(DoInteractCamera(npc));
    }

    private IEnumerator DoInteractCamera(GameObject npc)
    {
        var previousCamera = _currentCamera;
        SetActiveCamera(_interactCamera, _currentCamera);

        // Get the interact camera's transform
        Transform camTransform = _interactCamera.transform;

        // Step 1: place in front of player, look at player
        Vector3 playerPos = _playerTransform.position;
        Vector3 playerForward = _playerTransform.forward;
        camTransform.position = playerPos + playerForward * 0.5f + Vector3.up * 0.1f; // move forward & lift a bit;// move forward & lift a bit
        camTransform.LookAt(playerPos);
        yield return new WaitForSeconds(4f);

        // Step 2: place in front of NPC, look at NPC
        Transform npcTransform = npc.transform;
        Vector3 npcPos = npcTransform.position;
        Vector3 npcForward = npcTransform.forward;
        camTransform.position = npcPos + npcForward * 0.2f + Vector3.up * 0.1f; ; // move forward & lift a bit
        camTransform.LookAt(npcPos);

        // Step 3: Move NPC
        NPCController npcController = npc.GetComponent<NPCController>();
        npcController.Interact();
        yield return new WaitForSeconds(2f);

        // Step 4: return to previous camera
        SetActiveCamera(previousCamera, _interactCamera);
    }
}
