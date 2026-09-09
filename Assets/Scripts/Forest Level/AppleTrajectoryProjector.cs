using UnityEngine;

/// <summary>
/// Trajectory Projector should project the ground marker when apple is airborne
/// the ground marker(flat cylinder) is a rough estimate of where the apple will land. meaning the apple should land somewhere on the marker
///  
/// </summary>
public class AppleTrajectoryProjector : MonoBehaviour
{
    [SerializeField] GameObject GroundMarker;

    public void Project(Transform appleTransform, Rigidbody appleRigidbody)
    {
        GroundMarker.SetActive(true);
        Vector3 pos = appleTransform.position;
        Vector3 vel = appleRigidbody.linearVelocity;

        // Solve for time-to-ground using vertical SUVAT:
        // y(t) = pos.y + vel.y*t + 0.5*g*t^2 = groundY
        // Rearrange into a quadratic and solve for t, then:
        Vector3 landingPoint = Vector3.zero /* pos.xz + vel.xz * t */;

        GroundMarker.transform.position = landingPoint;
        
    }

}
