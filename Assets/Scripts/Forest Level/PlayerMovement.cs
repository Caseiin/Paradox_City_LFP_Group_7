using UnityEngine;
using UnityEngine.InputSystem;

namespace Forestlevel
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraTransform;

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float sprintMultiplier = 1.6f;
        public float acceleration = 10f;
        public float airControl = 2f;
        public float groundFriction = 8f;
        public float airFriction = 0.5f;

        [Header("Slope / Slide")]
        public float slopeLimit = 45f;
        public float slideFriction = 1f;
        public float slideGravity = 12f;

        [Header("Ground Check")]
        public float groundCheckDistance = 0.3f;
        public LayerMask groundMask = ~0;

        [Header("Look")]
        public float lookSensitivity = 2f;
        public float minPitch = -80f, maxPitch = 80f;

        Rigidbody _rb;
        public Rigidbody Rb => _rb;

        CapsuleCollider _col;
        Vector3 momentum;
        bool isGrounded;
        Vector3 groundNormal = Vector3.up;
        float pitch;

        // --- New Input System ---
        DefaultInputSystem controls;
        Vector2 moveInput;
        Vector2 lookInput;
        bool sprintHeld;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _rb.freezeRotation = true;
            Cursor.lockState = CursorLockMode.Locked;

            controls = new DefaultInputSystem();
        }

        void OnEnable()
        {
            controls.Player.Enable();

            controls.Player.Move.performed += OnMove;
            controls.Player.Move.canceled += OnMove;

            controls.Player.Look.performed += OnLook;
            controls.Player.Look.canceled += OnLook;

            controls.Player.Sprint.performed += OnSprint;
            controls.Player.Sprint.canceled += OnSprint;

            controls.Player.Jump.performed += OnJump;
        }

        void OnDisable()
        {
            controls.Player.Move.performed -= OnMove;
            controls.Player.Move.canceled -= OnMove;

            controls.Player.Look.performed -= OnLook;
            controls.Player.Look.canceled -= OnLook;

            controls.Player.Sprint.performed -= OnSprint;
            controls.Player.Sprint.canceled -= OnSprint;

            controls.Player.Jump.performed -= OnJump;

            controls.Player.Disable();
        }

        void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
        void OnLook(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
        void OnSprint(InputAction.CallbackContext ctx) => sprintHeld = ctx.ReadValueAsButton();
        void OnJump(InputAction.CallbackContext ctx)
        {
            // hook your jump logic here if/when you add it
        }

        void Update()
        {
            HandleLook();
        }

        void FixedUpdate()
        {
            Debug.Log($"grounded:{isGrounded} moveInput:{moveInput}");
            CheckGround();
            HandleMomentum();
            _rb.linearVelocity = momentum;
        }

        // ---------- LOOK ----------
        void HandleLook()
        {
            float mouseX = lookInput.x * lookSensitivity;
            float mouseY = lookInput.y * lookSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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

        // ---------- MOVEMENT ----------
        Vector3 GetMoveDirection()
        {
            Vector3 forward = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                : transform.forward;
            Vector3 right = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized
                : transform.right;

            Vector3 dir = forward * moveInput.y + right * moveInput.x;
            return dir.magnitude > 1f ? dir.normalized : dir;
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
                horizontal = Vector3.MoveTowards(horizontal, Vector3.ProjectOnPlane(horizontal, groundNormal),
                    slideFriction * Time.deltaTime);
            }
            else
            {
                Vector3 wishDir = GetMoveDirection();
                float speed = moveSpeed * (sprintHeld ? sprintMultiplier : 1f);
                Vector3 targetVel = wishDir * speed;
                float rate = isGrounded ? acceleration : airControl;
                horizontal = Vector3.MoveTowards(horizontal, targetVel, rate * Time.deltaTime);

                float friction = isGrounded ? groundFriction : airFriction;
                if (wishDir.sqrMagnitude < 0.01f)
                    horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, friction * Time.deltaTime);
            }

            momentum = horizontal + vertical;
        }
    }
}