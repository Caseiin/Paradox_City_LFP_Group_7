using Forestlevel;
using UnityEngine;
using Cinemachine;

public class CameraModeSwitcher : MonoBehaviour
{
    [SerializeField] CinemachineFreeLook  freeLookCam;
    [SerializeField] CinemachineVirtualCamera lockedFollowCam;
    [SerializeField] PlayerMovement playerMovement; // whatever exposes current move input/velocity
    [SerializeField] float moveThreshold = 0.1f;

    void Update()
    {
        bool isMoving = playerMovement.Rb.linearVelocity.sqrMagnitude > moveThreshold * moveThreshold;

        freeLookCam.Priority = isMoving ? 0 : 10;
        lockedFollowCam.Priority = isMoving ? 10 : 0;
    }
}