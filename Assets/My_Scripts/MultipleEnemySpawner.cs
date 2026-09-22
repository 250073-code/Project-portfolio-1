using UnityEngine;

namespace My_Scripts
{
    public class MultipleEnemySpawner : MonoBehaviour
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

        [Header("Настройки толпы")]
        [SerializeField] private int _minPackSize = 1;
        [SerializeField] private int _maxPackSize = 3;
        [SerializeField] private float _packSpreadRadius = 1f;

        private float _nextSpawnTime;
        private int _spawnedCount;
        private Transform _playerTransform;

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) _playerTransform = player.transform;

            _nextSpawnTime = Time.time + Random.Range(0f, _spawnCooldown);
        }

        private void Update()
        {
            if (_spawnedCount >= _maxSpawnsFromThisPoint) return;
            if (Time.time < _nextSpawnTime) return;

            if (!IsPlayerInSpawnRange())
            {
                _nextSpawnTime = Time.time + _retryDelay;
                return;
            }

            _nextSpawnTime = Time.time + _spawnCooldown;
            SpawnPack();
        }

        private bool IsPlayerInSpawnRange()
        {
            if (_playerTransform == null) return true;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            return distanceToPlayer >= _minSpawnDistance && distanceToPlayer <= _maxSpawnDistance;
        }

        private void SpawnPack()
        {
            int packSize = Random.Range(_minPackSize, _maxPackSize + 1);
            int actuallySpawned = 0;

            for (int i = 0; i < packSize && _spawnedCount < _maxSpawnsFromThisPoint; i++)
            {
                if (TrySpawnOne())
                {
                    actuallySpawned++;
                }
            }

            Debug.Log($"Точка {gameObject.name} заспавнила пачку из {actuallySpawned} врагов!");
        }

        private bool TrySpawnOne()
        {
            GameObject prefabToSpawn = Random.value < _alphaSpawnChance
                ? _alphaParasitePrefab
                : _regularParasitePrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"{name}: chosen prefab is not assigned - skipping spawn without using a slot.");
                return false;
            }

            Vector3 offset = new Vector3(
                Random.Range(-_packSpreadRadius, _packSpreadRadius),
                0,
                Random.Range(-_packSpreadRadius, _packSpreadRadius));

            Instantiate(prefabToSpawn, transform.position + offset, transform.rotation);
            _spawnedCount++;
            return true;
        }
    }
}