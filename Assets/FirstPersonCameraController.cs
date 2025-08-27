using UnityEngine;
using Cinemachine;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerBody; // The root object of your player
    [SerializeField] private CinemachineVirtualCamera firstPersonCam;

    private CinemachinePOV pov;

    void Start()
    {
        // Get the POV component from this vcam
        pov = firstPersonCam.GetCinemachineComponent<CinemachinePOV>();

        // Lock cursor to screen center
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (pov != null && playerBody != null)
        {
            // Rotate player body using the horizontal axis (yaw)
            playerBody.rotation = Quaternion.Euler(0f, pov.m_HorizontalAxis.Value, 0f);
        }
    }

    public void SetYawFromPlayer(float yaw)
    {
        if (pov != null)
        {
            pov.m_HorizontalAxis.Value = yaw;
        }
    }
}
