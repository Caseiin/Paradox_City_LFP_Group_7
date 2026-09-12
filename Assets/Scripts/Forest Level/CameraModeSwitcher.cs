// CameraRig.cs (renamed from CameraModeSwitcher — it no longer computes any
// state of its own; it's purely a consumer of PlayerMovement.IsLocked)
using UnityEngine;
using Cinemachine;

namespace Forestlevel
{
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] CinemachineFreeLook freeLookCam;
        [SerializeField] CinemachineVirtualCamera lockedFollowCam;
        [SerializeField] PlayerMovement playerMovement;

        void Update()
        {
            bool locked = playerMovement.IsLocked;

            freeLookCam.Priority = locked ? 0 : 10;
            lockedFollowCam.Priority = locked ? 10 : 0;
        }
    }
}