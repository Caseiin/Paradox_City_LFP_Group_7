using UnityEngine;
using UnityEngine.InputSystem;

namespace Forestlevel
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        // The active render camera, NOT a specific vcam — Cinemachine blends
        // multiple vcams into Camera.main, so reading from the vcam directly
        // would miss blends/cuts. This is what makes movement "camera-relative"
        // regardless of which vcam is currently live.
        [SerializeField] Transform cameraTransform;

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float acceleration = 10f;
        public float airControl = 2f;
        public float groundFriction = 8f;
        public float airFriction = 0.5f;

        [Header("Rotation")]
        // Fall Guys' character turns to face movement, independent of camera
        // facing. Degrees/sec, not a 0-1 lerp — keeps turn speed consistent
        // regardless of framerate or how close the angle already is.
        public float turnSpeed = 720f;

        [Header("Slope / Slide")]
        public float slopeLimit = 45f;
        public float slideGravity = 12f;

        [Header("Slide (damping-based)")]
        public float slideImpulse = 8f;       // forward push applied once, on slide start
        public float slideLinearDamping = 4f; // Rigidbody drag while sliding
        public float slideAngularDamping = 8f;
        float defaultLinearDamping;
        float defaultAngularDamping;
        bool isSliding;

        [Header("Ground Check")]
        public float groundCheckDistance = 0.3f;
        public LayerMask groundMask;

        Rigidbody _rb;
        public Rigidbody Rb => _rb;
        CapsuleCollider _col;

        Vector3 momentum;
        bool isGrounded;
        Vector3 groundNormal = Vector3.up;

        DefaultInputSystem controls;
        Vector2 moveInput;
        bool sprintHeld;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _rb.freezeRotation = true; // we rotate the transform manually, not via physics torque
            _rb.useGravity = false;    // gravity is handled manually in HandleMomentum

            defaultLinearDamping = _rb.linearDamping;
            defaultAngularDamping = _rb.angularDamping;

            controls = new DefaultInputSystem();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void OnEnable()
        {
            controls.Player.Enable();
            controls.Player.Move.performed += OnMove;
            controls.Player.Move.canceled += OnMove;
            controls.Player.Sprint.performed += OnSprint;
            controls.Player.Sprint.canceled += OnSprint;
            // Wire a Slide action the same way once it exists in the asset:
            // controls.Player.Slide.performed += _ => StartSlide();
            // controls.Player.Slide.canceled += _ => StopSlide();
        }

        void OnDisable()
        {
            controls.Player.Move.performed -= OnMove;
            controls.Player.Move.canceled -= OnMove;
            controls.Player.Sprint.performed -= OnSprint;
            controls.Player.Sprint.canceled -= OnSprint;
            controls.Player.Disable();
        }

        void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
        void OnSprint(InputAction.CallbackContext ctx) => sprintHeld = ctx.ReadValueAsButton();

        void FixedUpdate()
        {
            CheckGround();
            HandleMomentum();
            HandleFacing();
            _rb.linearVelocity = momentum;
        }

        // ---------- GROUND CHECK ----------
        void CheckGround()
        {
            float radius = _col.radius * 0.9f;
            Vector3 origin = _col.bounds.center;
            float castDistance = _col.bounds.extents.y - radius + groundCheckDistance;

            isGrounded = Physics.SphereCast(
                origin, radius, Vector3.down, out RaycastHit hit,
                castDistance, groundMask, QueryTriggerInteraction.Ignore);

            groundNormal = isGrounded ? hit.normal : Vector3.up;
        }

        bool IsTooSteep() => isGrounded && Vector3.Angle(groundNormal, Vector3.up) > slopeLimit;

        // ---------- CAMERA-RELATIVE MOVE DIRECTION ----------
        Vector3 GetMoveDirection()
        {
            if (cameraTransform == null) return Vector3.zero;

            // Flatten camera forward/right onto the ground plane so pitching
            // the camera up/down (looking at the sky) doesn't slow horizontal
            // movement or add unwanted vertical velocity.
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

            Vector3 dir = forward * moveInput.y + right * moveInput.x;
            return dir.magnitude > 1f ? dir.normalized : dir;
        }

        // ---------- FACING (independent of camera) ----------
        void HandleFacing()
        {
            // Only turn when there's actual movement intent and enough speed
            // to matter — this avoids the character twitching to face tiny
            // residual momentum as it comes to a stop.
            Vector3 flatVelocity = Vector3.ProjectOnPlane(momentum, Vector3.up);
            if (flatVelocity.sqrMagnitude < 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(flatVelocity, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(_rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(newRot);
        }

        // ---------- MOMENTUM ----------
        void HandleMomentum()
        {
            Vector3 vertical = Vector3.Project(momentum, Vector3.up);
            Vector3 horizontal = momentum - vertical;

            vertical -= Vector3.up * (Physics.gravity.magnitude * Time.deltaTime);
            if (isGrounded && !IsTooSteep() && Vector3.Dot(vertical, Vector3.up) < 0f)
                vertical = Vector3.zero;

            if (IsTooSteep())
            {
                horizontal = Vector3.ProjectOnPlane(horizontal, groundNormal);
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                horizontal += slideDir * (slideGravity * Time.deltaTime);
            }
            else if (!isSliding)
            {
                // Sliding is driven entirely by damping + the initial impulse
                // (see StartSlide), so normal move-acceleration is skipped
                // while sliding — otherwise input would just cancel the slide.
                Vector3 wishDir = GetMoveDirection();
                float speed = moveSpeed * (sprintHeld ? 1.6f : 1f);
                Vector3 targetVel = wishDir * speed;
                float rate = isGrounded ? acceleration : airControl;
                horizontal = Vector3.MoveTowards(horizontal, targetVel, rate * Time.deltaTime);

                float friction = isGrounded ? groundFriction : airFriction;
                if (wishDir.sqrMagnitude < 0.01f)
                    horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, friction * Time.deltaTime);
            }

            momentum = horizontal + vertical;
        }

        // ---------- SLIDE ----------
        public void StartSlide()
        {
            if (isSliding || !isGrounded) return;
            isSliding = true;

            _rb.linearDamping = slideLinearDamping;
            _rb.angularDamping = slideAngularDamping;

            // One-off push in the current facing direction. Damping then
            // bleeds this off naturally over time instead of you having to
            // author a deceleration curve by hand.
            momentum += transform.forward * slideImpulse;
        }

        public void StopSlide()
        {
            if (!isSliding) return;
            isSliding = false;

            _rb.linearDamping = defaultLinearDamping;
            _rb.angularDamping = defaultAngularDamping;
        }
    }
}