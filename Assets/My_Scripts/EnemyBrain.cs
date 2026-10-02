using Cysharp.Threading.Tasks;
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

        [Header("Ranges")]
        [SerializeField] private float _attackRange = 2.5f;
        [SerializeField] private float _chaseRange = 15f;
        [SerializeField] private float _jumpRange = 7f;
        [SerializeField] private float _minJumpRange = 4f; // closer than this: chase/melee instead of leaping in place
        [SerializeField] private float _landJumpDistance = 5f;

        [Header("Timing")]
        [SerializeField] private float _jumpCoolDown = 7f;

        [Header("Jump Leap (tune to match the JumpAttack clip)")]
        [SerializeField] private float _jumpWindupTime = 0.4f;  // crouch before takeoff
        [SerializeField] private float _jumpAirTime = 0.6f;     // how long the leap lasts
        [SerializeField] private float _jumpLandShortBy = 1.5f; // land this far before the player's position

        [Header("Combat")]
        [SerializeField] private float _attackDamage = 10f;
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
        private PlayerController _playerController;
        private IDamageable _playerDamageable;
        private Cinemachine.CinemachineImpulseSource _impulseSource;

        private State _state = State.Idle;
        private bool _isDead;
        private float _nextJumpTime;

        // Squared ranges avoid a sqrt every frame from Vector3.Distance
        private float _attackRangeSqr;
        private float _chaseRangeSqr;
        private float _jumpRangeSqr;
        private float _minJumpRangeSqr;
        private float _landJumpDistanceSqr;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _impulseSource = GetComponentInChildren<Cinemachine.CinemachineImpulseSource>();

            _attackRangeSqr = _attackRange * _attackRange;
            _chaseRangeSqr = _chaseRange * _chaseRange;
            _jumpRangeSqr = _jumpRange * _jumpRange;
            _minJumpRangeSqr = _minJumpRange * _minJumpRange;
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
            _playerController = playerObj.GetComponent<PlayerController>();
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

            // Player is dead: stop chasing/attacking (ParasiteBrain already did this, this one didn't)
            if (_playerController != null && _playerController.IsDead)
            {
                EnterIdle();
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
            else if (distanceSqr <= _jumpRangeSqr && distanceSqr >= _minJumpRangeSqr && Time.time >= _nextJumpTime)
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
            RotateTowardsPlayer();

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

            // NavMeshAgent only rotates the transform while actively moving (Chasing).
            // If the jump triggers straight from Idle (player suddenly within jump range
            // without a chase in between), the enemy is still facing wherever it was
            // before and the leap fires in that stale direction. Snap-face the player now.
            SnapRotateTowardsPlayer();

            _animator.SetTrigger(HashJumpAttack);
            _nextJumpTime = Time.time + _jumpCoolDown;
            LeapTowardsPlayer().Forget();
        }

        private async UniTaskVoid LeapTowardsPlayer()
        {
            var ct = this.GetCancellationTokenOnDestroy();

            // Wait out the crouch/wind-up part of the animation before taking off.
            await UniTask.Delay(System.TimeSpan.FromSeconds(_jumpWindupTime), cancellationToken: ct);
            if (_isDead || _state != State.Jumping) return;

            // The landing point is fixed at takeoff (not tracked live), so the player can dodge.
            Vector3 start = transform.position;
            Vector3 toPlayer = _player.position - start;
            toPlayer.y = 0f;

            float distance = Mathf.Max(0f, toPlayer.magnitude - _jumpLandShortBy);
            Vector3 target = start + toPlayer.normalized * distance;

            Debug.Log($"{name}: leap distance {distance:F1}m over {_jumpAirTime:F1}s"); // temporary, remove when tuned

            // Take the NavMeshAgent out of the picture while airborne so nothing fights the movement
            // (isStopped, path following, root motion snap-back). It is restored in finally.
            _agent.enabled = false;
            try
            {
                float elapsed = 0f;
                while (elapsed < _jumpAirTime && !_isDead)
                {
                    elapsed += Time.deltaTime;
                    transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / _jumpAirTime));
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            finally
            {
                // `this != null` is Unity's destroyed-object check (covers cancellation on destroy)
                if (this != null && !_isDead)
                {
                    ResumeAgentAt(transform.position);
                }
            }
        }

        private void ResumeAgentAt(Vector3 position)
        {
            _agent.enabled = true;

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }

            // Still mid-jump-animation: stay stopped until the OnJumpAnimationEvent releases us.
            _agent.isStopped = _state == State.Jumping;
        }

        private void RotateTowardsPlayer()
        {
            Vector3 direction = _player.position - transform.position;
            direction.y = 0;
            if (direction == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        private void SnapRotateTowardsPlayer()
        {
            Vector3 direction = _player.position - transform.position;
            direction.y = 0;
            if (direction == Vector3.zero) return;

            transform.rotation = Quaternion.LookRotation(direction);
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

            // Agent is disabled while the scripted leap is airborne; ResumeAgentAt handles it then.
            if (_agent.enabled)
            {
                _agent.isStopped = false;
            }
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