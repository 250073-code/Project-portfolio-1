using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace My_Scripts
{
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Visual Effects")]
        [SerializeField] private SkinnedMeshRenderer _meshRenderer;
        [SerializeField] private Color _flashColor = Color.red;
        [SerializeField] private float _flashDuration = 0.1f;
        private Color _originalColor;

        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;

        [Header("Boss Settings")]
        [SerializeField] private string _bossName;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _hurtSound; 
        private float _currentHealth;
        private bool IsBoss => !string.IsNullOrEmpty(_bossName);

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        [Header("Events (Архитектура)")]
        public UnityEvent<string> OnBossActivated;
        public UnityEvent<float, float> OnHealthChanged;
        public UnityEvent OnTakeDamage;
        public UnityEvent OnDeath;
        
        private CancellationTokenSource _flashCts;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            if (_meshRenderer != null) _originalColor = _meshRenderer.material.color;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void OnDestroy()
        {
            _flashCts?.Cancel();
            _flashCts?.Dispose();
        }

        public void TakeDamage(float damage)
        {
            if (_currentHealth <= 0) return;

            _currentHealth -= damage;
            Debug.Log($"{gameObject.name} получил урон. ХП: {_currentHealth}");

            RestartFlash();
            PlaySound(_hurtSound);

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnTakeDamage?.Invoke();

            if (_currentHealth <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void RestoreHealth(float amount)
        {
            if (_currentHealth <= 0) return;

            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            Debug.Log($"{gameObject.name} вылечился. ХП: {_currentHealth}");

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void ShowBossHealthBar()
        {
            if (!IsBoss) return;

            OnBossActivated?.Invoke(_bossName);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void RestartFlash()
        {
            if (_meshRenderer == null) return;

            _flashCts?.Cancel();
            _flashCts?.Dispose();
            _flashCts = new CancellationTokenSource();

            FlashRoutine(_flashCts.Token).Forget();
        }

        private async UniTaskVoid FlashRoutine(CancellationToken token)
        {
            _meshRenderer.material.color = _flashColor;

            bool cancelled = await UniTask.WaitForSeconds(_flashDuration, cancellationToken: token)
                .SuppressCancellationThrow();

            if (!cancelled)
            {
                _meshRenderer.material.color = _originalColor;
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