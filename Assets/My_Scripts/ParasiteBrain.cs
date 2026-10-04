using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace My_Scripts
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class ParasiteBrain : MonoBehaviour
    {
        private enum State
        {
            Patrolling,
            Chasing,
            Attacking,
            Dead
        }

        [Header("Target Settings")]
        [SerializeField] private Transform _player;

        [Header("AI Parameters")]
        [SerializeField] private float _viewRadius = 10.0f;
        [SerializeField] private float _attackRange = 1.8f;
        [SerializeField] private float _patrolRadius = 5.0f;
        [SerializeField] private float _waitTime = 2.0f;

        [Header("Attack Settings")]
        [SerializeField] private float _attackCooldown = 0.5f;
        [SerializeField] private float _damageAmount = 10f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _hitSound; 

        private static readonly int HashIsChasing = Animator.StringToHash("isChasing");
        private static readonly int HashIsWalking = Animator.StringToHash("isWalking");
        private static readonly int HashAttackState = Animator.StringToHash("Mutant Swiping");

        private NavMeshAgent _agent;
        private Animator _animator;
        private PlayerController _playerController;
        private IDamageable _playerDamageable;

        private Vector3 _startPosition;
        private float _waitTimer;
        private float _lastAttackTime;

        private State _state = State.Patrolling;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            if (_player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _player = playerObject.transform;
                }
            }

            if (_player != null)
            {
                _playerController = _player.GetComponent<PlayerController>();
                _playerDamageable = _player.GetComponent<IDamageable>();
            }

            _startPosition = transform.position;
        }

        private void Update()
        {
            if (_state == State.Dead || _state == State.Attacking) return;
            if (_playerController != null && _playerController.IsDead) return;
            if (_player == null) return;

            CalculateAIBehavior();
        }

        private void CalculateAIBehavior()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            if (distanceToPlayer <= _attackRange)
            {
                HandleAttackState();
            }
            else if (distanceToPlayer <= _viewRadius)
            {
                HandleChaseState();
            }
            else
            {
                Patrol();
            }

            UpdateAnimatorState(distanceToPlayer);
        }

        private void HandleAttackState()
        {
            RotateTowardsPlayer();

            if (_agent.enabled)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }

            bool cooldownOver = Time.time >= _lastAttackTime + _attackCooldown;
            if (!cooldownOver) return;

            _lastAttackTime = Time.time;
            _state = State.Attacking; 
            _animator.Play(HashAttackState, 0, 0f);
            
        }

        private void HandleChaseState()
        {
            _state = State.Chasing;
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
        }

        private void Patrol()
        {
            _state = State.Patrolling;

            if (_agent.remainingDistance > _agent.stoppingDistance)
            {
                return;
            }

            _waitTimer += Time.deltaTime;
            if (_waitTimer < _waitTime) return;

            Vector3 randomPoint = transform.position + Random.insideUnitSphere * _patrolRadius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                _waitTimer = 0;
            }
        }

        /// <summary>Animation event: add this at the end of the attack animation clip.</summary>
        public void OnAttackAnimationFinished()
        {
            if (_state == State.Dead) return;

            _state = State.Patrolling;
            ResumeAgentNextFrame().Forget();
        }
        
        private async UniTaskVoid ResumeAgentNextFrame()
        {
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());

            if (_state == State.Dead || _agent == null || !_agent.enabled) return;
            _agent.isStopped = false;
        }

        public void Die()
        {
            if (_state == State.Dead) return;
            _state = State.Dead;

            _agent.isStopped = true;
            _agent.enabled = false;

            _animator.CrossFadeInFixedTime("Dying Backwards (1)", 0.1f);

            Destroy(gameObject, 3.0f);
            enabled = false;
        }

        private void RotateTowardsPlayer()
        {
            Vector3 playerDirection = (_player.position - transform.position).normalized;
            playerDirection.y = 0;

            if (playerDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(playerDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10.0f);
            }
        }

        private void UpdateAnimatorState(float distanceToPlayer)
        {
            bool isMoving = _agent.velocity.magnitude > 0.1f;

            if (distanceToPlayer <= _attackRange)
            {
                _animator.SetBool(HashIsChasing, false);
                _animator.SetBool(HashIsWalking, false);
            }
            else if (distanceToPlayer <= _viewRadius)
            {
                _animator.SetBool(HashIsChasing, isMoving);
                _animator.SetBool(HashIsWalking, false);
            }
            else
            {
                _animator.SetBool(HashIsChasing, false);
                _animator.SetBool(HashIsWalking, isMoving);
            }
        }

        public void HitPlayer()
        {
            PlaySound(_hitSound);
            if (_playerDamageable == null) return;

            float distance = Vector3.Distance(transform.position, _player.position);
            if (distance <= _attackRange + 0.5f)
            {
                _playerDamageable.TakeDamage(_damageAmount);
            }
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}