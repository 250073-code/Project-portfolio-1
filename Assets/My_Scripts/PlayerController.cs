using UnityEngine;

namespace My_Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _groundedStickVelocity = -2f;
        [SerializeField] private float _lookPitchClamp = 70f;

        public bool IsDead { get; private set; }

        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Camera _mainCam;

        [Header("Grenade Physics")]
        [SerializeField] private GameObject _grenadePrefab;
        [SerializeField] private Transform _throwPoint;
        [SerializeField] private float _throwForce = 15f;
        [SerializeField] private int _grenadeCount = 3;

        private static readonly int HashIsRunning = Animator.StringToHash("isRunning");

        private CharacterController _controller;
        private PlayerControls _inputs;
        private UIManager _ui;

        private float _xRotation;
        private float _verticalVelocity;
        private Vector2 _movementInput;
        private Vector2 _lookInput;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            _inputs = new PlayerControls();
            _inputs.GamePlay.Grenade.performed += OnGrenadePerformed;

            if (_mainCam == null)
            {
                Debug.LogWarning($"{name}: no camera assigned - mouse look will be disabled.");
            }
            if (_animator == null)
            {
                Debug.LogWarning($"{name}: no Animator assigned - movement animation will be skipped.");
            }
        }

        private void OnEnable() => _inputs.Enable();
        private void OnDisable() => _inputs.Disable();

        private void OnDestroy()
        {
            _inputs.GamePlay.Grenade.performed -= OnGrenadePerformed;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _ui = UIManager.Instance;
            _ui?.UpdateGrenadeUI(_grenadeCount);
        }

        private void Update()
        {
            if (IsDead) return;

            _movementInput = _inputs.Move.Move.ReadValue<Vector2>();
            _lookInput = _inputs.Move.MouseRotation.ReadValue<Vector2>();

            RotateTowardsMouse();
            CalculateMovement();
        }

        private void RotateTowardsMouse()
        {
            if (_mainCam == null) return;

            float mouseX = _lookInput.x * _mouseSensitivity;
            float mouseY = _lookInput.y * _mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -_lookPitchClamp, _lookPitchClamp);
            _mainCam.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }

        private void CalculateMovement()
        {
            if (_controller.isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = _groundedStickVelocity;
            }
            else
            {
                _verticalVelocity += _gravity * Time.deltaTime;
            }

            // Clamp instead of normalize: preserves partial gamepad stick input while
            // preventing the classic bug where diagonal (x=1,y=1) input moves ~41% faster.
            Vector3 moveDirection = transform.right * _movementInput.x + transform.forward * _movementInput.y;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

            Vector3 movement = (moveDirection * _speed) + (Vector3.up * _verticalVelocity);
            _controller.Move(movement * Time.deltaTime);

            if (_animator != null)
            {
                bool isMoving = moveDirection.sqrMagnitude > 0.1f;
                _animator.SetBool(HashIsRunning, isMoving);
            }
        }

        private void OnGrenadePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            ThrowGrenade();
        }

        private void ThrowGrenade()
        {
            if (IsDead || _grenadeCount <= 0) return;

            if (_grenadePrefab == null || _throwPoint == null)
            {
                Debug.LogError($"{name}: grenade prefab or throw point is not assigned.");
                return;
            }

            GameObject grenade = Instantiate(_grenadePrefab, _throwPoint.position, _throwPoint.rotation);
            if (grenade.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForce(_throwPoint.forward * _throwForce, ForceMode.Impulse);
            }

            _grenadeCount--;
            _ui?.UpdateGrenadeUI(_grenadeCount);
        }

        public void Die()
        {
            IsDead = true;
            GameManager.Instance?.TriggerGameOver();
        }
    }
}