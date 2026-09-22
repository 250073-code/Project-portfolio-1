using UnityEngine;

namespace My_Scripts
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Префабы врагов")]
        [SerializeField] private GameObject _regularParasitePrefab;
        [SerializeField] private GameObject _alphaParasitePrefab;

        [Header("Настройки")]
        [SerializeField] private float _spawnCooldown = 10f;
        [SerializeField] private float _retryDelay = 1f; // Как скоро перепроверить, если спавн был пропущен по дистанции
        [SerializeField] private int _maxSpawnsFromThisPoint = 5;
        [SerializeField] private float _alphaSpawnChance = 0.2f;
        [SerializeField] private float _minSpawnDistance = 10f;
        [SerializeField] private float _maxSpawnDistance = 50f;

        private float _nextSpawnTime;
        private int _spawnedCount;
        private Transform _playerTransform;

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) _playerTransform = player.transform;

            // Random initial delay so every spawner in the scene doesn't fire in sync
            _nextSpawnTime = Time.time + Random.Range(0f, _spawnCooldown);
        }

        private void Update()
        {
            if (_spawnedCount >= _maxSpawnsFromThisPoint) return;
            if (Time.time < _nextSpawnTime) return;

            if (!IsPlayerInSpawnRange())
            {
                // Player was too close/far - retry soon instead of burning the full cooldown
                _nextSpawnTime = Time.time + _retryDelay;
                return;
            }

            _nextSpawnTime = Time.time + _spawnCooldown;
            SpawnEnemy();
        }

        private bool IsPlayerInSpawnRange()
        {
            if (_playerTransform == null) return true; // no player to check against yet

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            return distanceToPlayer >= _minSpawnDistance && distanceToPlayer <= _maxSpawnDistance;
        }

        private void SpawnEnemy()
        {
            GameObject prefabToSpawn = Random.value < _alphaSpawnChance
                ? _alphaParasitePrefab
                : _regularParasitePrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"{name}: chosen prefab is not assigned - skipping spawn without using a slot.");
                return;
            }

            // ParasiteBrain finds the player itself in Awake() if not assigned, so no
            // reference needs to be passed in here.
            Instantiate(prefabToSpawn, transform.position, transform.rotation);
            _spawnedCount++;
        }
    }
}