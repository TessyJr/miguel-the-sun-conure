using UnityEngine;
using Cinemachine;

public class FirstPersonController : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private CinemachineVirtualCamera firstPersonVcam;

    private CinemachinePOV pov;

    void Start()
    {
        pov = firstPersonVcam.GetCinemachineComponent<CinemachinePOV>();
        Cursor.lockState = CursorLockMode.Locked; // lock mouse
    }

    void Update()
    {
        // Rotate the player body based on horizontal axis (yaw)
        if (pov != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, pov.m_HorizontalAxis.Value, 0f);
        }
    }
}
