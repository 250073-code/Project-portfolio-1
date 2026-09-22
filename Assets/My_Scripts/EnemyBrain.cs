using UnityEngine;
using UnityEngine.AI;

namespace My_Scripts
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyBrain : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Chasing,
            Attacking,
            Jumping
        }

        [Header("Ranges")] [SerializeField] private float _attackRange = 2.5f;
        [SerializeField] private float _chaseRange = 15f;
        [SerializeField] private float _jumpRange = 7f;
        [SerializeField] private float _landJumpDistance = 5f;

        [Header("Timing")] [SerializeField] private float _jumpCoolDown = 7f;

        [Header("Combat")] [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _jumpLandingDamage = 20f;
        [SerializeField] private float _deathDestroyDelay = 4f;

        // Cached animator parameter hashes (cheaper than string lookups every frame)
        private static readonly int HashIsRunning = Animator.StringToHash("isRunning");
        private static readonly int HashIsDead = Animator.StringToHash("isDead");
        private static readonly int HashAttack = Animator.StringToHash("Attack");
        private static readonly int HashDie = Animator.StringToHash("Die");
        private static readonly int HashJumpAttack = Animator.StringToHash("JumpAttack");

        private NavMeshAgent _agent;
        private Animator _animator;
        private Transform _player;
        private IDamageable _playerDamageable;
        private Cinemachine.CinemachineImpulseSource _impulseSource;

        private State _state = State.Idle;
        private bool _isDead;
        private float _nextJumpTime;

        // Squared ranges avoid a sqrt every frame from Vector3.Distance
        private float _attackRangeSqr;
        private float _chaseRangeSqr;
        private float _jumpRangeSqr;
        private float _landJumpDistanceSqr;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _impulseSource = GetComponentInChildren<Cinemachine.CinemachineImpulseSource>();

            _attackRangeSqr = _attackRange * _attackRange;
            _chaseRangeSqr = _chaseRange * _chaseRange;
            _jumpRangeSqr = _jumpRange * _jumpRange;
            _landJumpDistanceSqr = _landJumpDistance * _landJumpDistance;
        }

        private void Start()
        {
            _agent.stoppingDistance = _attackRange;

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError($"{name}: no GameObject tagged 'Player' found. Disabling brain.");
                enabled = false;
                return;
            }

            _player = playerObj.transform;
            _playerDamageable = playerObj.GetComponent<IDamageable>();
            if (_playerDamageable == null)
            {
                Debug.LogWarning($"{name}: player has no IDamageable component; damage calls will be skipped.");
            }
        }

        private void Update()
        {
            if (_isDead || _player == null || !_agent.isActiveAndEnabled)
            {
                return;
            }

            // Freeze all logic while the jump-attack animation is actually playing
            if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash == HashJumpAttack)
            {
                _agent.isStopped = true;
                return;
            }

            if (_state == State.Jumping)
            {
                return;
            }

            float distanceSqr = (transform.position - _player.position).sqrMagnitude;

            if (distanceSqr <= _attackRangeSqr)
            {
                EnterAttack();
            }
            else if (distanceSqr <= _jumpRangeSqr && Time.time >= _nextJumpTime)
            {
                EnterJump();
            }
            else if (distanceSqr <= _chaseRangeSqr)
            {
                EnterChase();
            }
            else
            {
                EnterIdle();
            }
        }

        private void EnterChase()
        {
            _state = State.Chasing;
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
            _animator.SetBool(HashIsRunning, true);
        }

        private void EnterAttack()
        {
            _state = State.Attacking;
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _animator.SetBool(HashIsRunning, false);

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            bool alreadyAttacking = stateInfo.IsName("Mutant Swiping") || _animator.IsInTransition(0);
            if (!alreadyAttacking)
            {
                _animator.SetTrigger(HashAttack);
            }
        }

        private void EnterJump()
        {
            _state = State.Jumping;
            _agent.isStopped = true;
            _animator.SetTrigger(HashJumpAttack);
            _nextJumpTime = Time.time + _jumpCoolDown;
        }

        private void EnterIdle()
        {
            _state = State.Idle;
            _agent.isStopped = true;
            _animator.SetBool(HashIsRunning, false);
        }

        /// <summary>Call this from the SMG/weapon script when the enemy is hit.</summary>
        public void TakeDamage()
        {
            if (_isDead) return;

            _isDead = true;
            _state = State.Idle;

            _agent.enabled = false; // stop nav so the corpse doesn't float
            _animator.ResetTrigger(HashAttack);
            _animator.SetBool(HashIsRunning, false);
            _animator.SetBool(HashIsDead, true);
            _animator.SetTrigger(HashDie);

            if (TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            Destroy(gameObject, _deathDestroyDelay);
        }

        /// <summary>Animation event: fired at the end of the jump-attack clip.</summary>
        public void OnJumpAnimationEvent()
        {
            _state = State.Idle;
            _agent.isStopped = false;
        }

        /// <summary>Animation event: fired on the swipe's hit frame.</summary>
        public void HitPlayer()
        {
            if (_playerDamageable == null) return;

            float distanceSqr = (transform.position - _player.position).sqrMagnitude;
            float reach = _attackRange + 0.5f;

            if (distanceSqr <= reach * reach)
            {
                _playerDamageable.TakeDamage(_attackDamage);
            }
        }

        /// <summary>Animation event: fired on landing from the jump attack.</summary>
        public void LandFromJump()
        {
            _impulseSource?.GenerateImpulse();

            if (_playerDamageable == null) return;

            float distanceSqr = (transform.position - _player.position).sqrMagnitude;
            if (distanceSqr <= _landJumpDistanceSqr)
            {
                _playerDamageable.TakeDamage(_jumpLandingDamage);
            }
        }
    }
}