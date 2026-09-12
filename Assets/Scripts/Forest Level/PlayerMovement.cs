// PlayerMovement.cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace Forestlevel
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraTransform; // used only while free-look (unlocked)

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float acceleration = 10f;
        public float airControl = 2f;
        public float groundFriction = 8f;
        public float airFriction = 0.5f;

        [Header("Rotation")]
        public float turnSpeed = 720f;

        [Header("Slope / Slide")]
        public float slopeLimit = 45f;
        public float slideGravity = 12f;

        [Header("Slide (damping-based)")]
        public float slideImpulse = 8f;
        public float slideLinearDamping = 4f;
        public float slideAngularDamping = 8f;
        float defaultLinearDamping;
        float defaultAngularDamping;
        bool isSliding;

        [Header("Ground Check")]
        public float groundCheckDistance = 0.3f;
        public LayerMask groundMask;

        [Header("Camera Lock State")]
        [SerializeField] float lockEnterSpeed = 0.15f;
        [SerializeField] float lockExitSpeed = 0.05f;
        public bool IsLocked { get; private set; }

        // Frozen heading captured the instant movement begins. wishDir is
        // built relative to THIS, never to the player's own live rotation —
        // that's what stops the target from chasing itself as the player turns.
        Quaternion lockedBasis = Quaternion.identity;

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
            _rb.freezeRotation = true;
            _rb.useGravity = false;

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
            UpdateLockState();
            CheckGround();
            HandleMomentum();
            HandleFacing();
            _rb.linearVelocity = momentum;
        }

        void UpdateLockState()
        {
            float speedSqr = _rb.linearVelocity.sqrMagnitude;
            bool wasLocked = IsLocked;

            if (!IsLocked && speedSqr > lockEnterSpeed * lockEnterSpeed) IsLocked = true;
            else if (IsLocked && speedSqr < lockExitSpeed * lockExitSpeed) IsLocked = false;

            if (IsLocked && !wasLocked)
            {
                // Just started moving — freeze whichever way we're currently
                // looking as the movement reference for this run. Captured
                // ONCE per lock-in, never recomputed from the player's own
                // rotation afterward.
                Vector3 flatForward = cameraTransform != null
                    ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up)
                    : transform.forward;

                if (flatForward.sqrMagnitude < 0.0001f)
                    flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

                lockedBasis = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            }
        }

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

        Vector3 GetMoveDirection()
        {
            Vector3 forward, right;

            if (IsLocked)
            {
                // Fixed, external reference — does NOT rotate as the player
                // turns to face wishDir. This is what lets RotateTowards
                // actually converge instead of chasing a moving target.
                forward = lockedBasis * Vector3.forward;
                right = lockedBasis * Vector3.right;
            }
            else
            {
                if (cameraTransform == null) return Vector3.zero;
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }

            Vector3 dir = forward * moveInput.y + right * moveInput.x;
            return dir.magnitude > 1f ? dir.normalized : dir;
        }

        void HandleFacing()
        {
            Vector3 flatVelocity = Vector3.ProjectOnPlane(momentum, Vector3.up);
            if (flatVelocity.sqrMagnitude < 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(flatVelocity, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(_rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(newRot);
        }

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

        public void StartSlide()
        {
            if (isSliding || !isGrounded) return;
            isSliding = true;

            _rb.linearDamping = slideLinearDamping;
            _rb.angularDamping = slideAngularDamping;

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