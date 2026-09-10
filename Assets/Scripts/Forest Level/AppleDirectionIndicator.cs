using UnityEngine;
using UnityEngine.UIElements;

// One indicator = one arrow VisualElement + the target it's pointing at.
// Self-contained: give it a target, call Tick() every frame, done.
public class AppleDirectionIndicator
{
    readonly VisualElement element;
    readonly Camera cam;
    readonly float angleThreshold;

    public Transform Target { get; private set; }
    public VisualElement Element => element;

    public AppleDirectionIndicator(VisualElement element, Camera cam, float angleThreshold)
    {
        this.element = element;
        this.cam = cam;
        this.angleThreshold = angleThreshold;

        element.style.position = Position.Absolute;

        // Pivot at the bottom-center of the box — this is the point that
        // stays glued to the anchor while the rest of the arrow swings.
        element.style.transformOrigin = new StyleTransformOrigin(
            new TransformOrigin(Length.Percent(50), Length.Percent(100))
        );

        // Absolute positioning places the box's *top-left corner* at (left, top)
        // by default — we actually want the box's bottom-center there instead.
        // We don't know the element's real size until layout runs once, so we
        // shift it after the first GeometryChangedEvent.
        element.style.left = Length.Percent(50);
        element.style.top = Length.Percent(50);
        element.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
    }

    void OnFirstLayout(GeometryChangedEvent evt)
    {
        element.style.marginLeft = -element.resolvedStyle.width / 2f;
        element.style.marginTop = -element.resolvedStyle.height;
        element.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
    }

    public void SetTarget(Transform target) => Target = target;

    public void Tick()
    {
        if (Target == null) return;

        Vector3 toTarget = Target.position - cam.transform.position;
        Vector3 targetDir = toTarget.normalized;
        float dot = Vector3.Dot(targetDir, cam.transform.forward);

        if (dot > angleThreshold)
        {
            element.style.display = DisplayStyle.None;
            return;
        }

        element.style.display = DisplayStyle.Flex;

        float x = Vector3.Dot(toTarget, cam.transform.right);
        float y = Vector3.Dot(toTarget, cam.transform.up);
        float angleDeg = Mathf.Atan2(y, x) * Mathf.Rad2Deg;

        element.style.rotate = new StyleRotate(new Rotate(new Angle(angleDeg, AngleUnit.Degree)));
    }
}
