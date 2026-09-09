using UnityEngine;

/// <summary>
/// Trajectory Projector should project the ground marker when apple is airborne
/// the ground marker(flat cylinder) is a rough estimate of where the apple will land. meaning the apple should land somewhere on the marker
///  
/// </summary>
public class AppleTrajectoryProjector : MonoBehaviour
{
    [SerializeField] GameObject GroundMarker;
    public void Project(Transform appleTransform, Rigidbody appleRigidbody){
        //Todo: Project the ground marker on the ground on the rough predicted drop point of the apple

    }

}