using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My_Scripts
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Health UI")]
        [SerializeField] private Image _healthBarFill;
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("Hit Feedback Settings")]
        [SerializeField] private RectTransform _healthUIContainer;
        [SerializeField] private float _shakeDuration = 0.2f;
        [SerializeField] private float _shakeMagnitude = 10f;
        [SerializeField] private Color _hitColor = Color.red;

        [Header("Weapon & Item UI")]
        [SerializeField] private TextMeshProUGUI _ammoText;
        [SerializeField] private TextMeshProUGUI _grenadeText;

        [Header("Boss UI")]
        [SerializeField] private GameObject _bossHealthPanel;
        [SerializeField] private Image _bossHealthBarFill;
        [SerializeField] private TextMeshProUGUI _bossNameText;

        private Vector3 _originalContainerPos;
        private Color _originalHeartColor;

        // Cancels only the hit-feedback tween, so unrelated future UI animations
        // on this object are never accidentally stopped alongside it.
        private CancellationTokenSource _hitFeedbackCts;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (_healthUIContainer != null)
                _originalContainerPos = _healthUIContainer.anchoredPosition;

            if (_healthBarFill != null)
                _originalHeartColor = _healthBarFill.color;
        }

        private void OnDestroy()
        {
            _hitFeedbackCts?.Cancel();
            _hitFeedbackCts?.Dispose();
        }

        /// <summary>Call from Health when damage is taken.</summary>
        public void PlayHitFeedback()
        {
            RestartHitFeedback();
        }

        /// <summary>Call from Health when HP changes.</summary>
        public void UpdateHealthUI(float currentHealth, float maxHealth)
        {
            if (_healthBarFill == null) return;

            _healthBarFill.fillAmount = SafeRatio(currentHealth, maxHealth);

            if (_healthText != null)
            {
                _healthText.text = $"HP: {Mathf.Max(0, Mathf.RoundToInt(currentHealth))}";
            }
        }

        /// <summary>Call from GunSystem when firing or reloading.</summary>
        public void UpdateAmmoUI(int currentAmmo, int maxAmmo)
        {
            if (_ammoText != null)
            {
                _ammoText.text = $"{currentAmmo}/{maxAmmo}";
            }
        }

        /// <summary>Call from PlayerController when throwing a grenade.</summary>
        public void UpdateGrenadeUI(int currentGrenades)
        {
            if (_grenadeText != null)
            {
                _grenadeText.text = $"Grenades: {currentGrenades}";
            }
        }

        public void ShowBossPanel(string bossName)
        {
            if (_bossHealthPanel != null)
            {
                _bossHealthPanel.SetActive(true);
            }

            if (_bossNameText != null)
            {
                _bossNameText.text = bossName;
            }
        }

        public void UpdateBossHealthUI(float currentHealth, float maxHealth)
        {
            if (_bossHealthBarFill != null)
            {
                _bossHealthBarFill.fillAmount = SafeRatio(currentHealth, maxHealth);
            }
        }

        /// <summary>Call when the boss dies, to remove the boss bar from screen.</summary>
        public void HideBossHealthUI()
        {
            if (_bossHealthPanel != null)
            {
                _bossHealthPanel.SetActive(false);
            }
        }

        public void StopHeartShake()
        {
            _hitFeedbackCts?.Cancel();
            _hitFeedbackCts?.Dispose();
            _hitFeedbackCts = null;
            ResetHitFeedbackUI();
        }

        private static float SafeRatio(float current, float max)
        {
            return max > 0f ? current / max : 0f;
        }

        private void RestartHitFeedback()
        {
            _hitFeedbackCts?.Cancel();
            _hitFeedbackCts?.Dispose();
            _hitFeedbackCts = new CancellationTokenSource();

            HitFeedbackRoutine(_hitFeedbackCts.Token).Forget();
        }

        private async UniTaskVoid HitFeedbackRoutine(CancellationToken token)
        {
            if (_healthBarFill != null) _healthBarFill.color = _hitColor;

            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                if (token.IsCancellationRequested) return;

                if (Time.timeScale != 0f) // skip advancing the shake while paused
                {
                    float x = Random.Range(-1f, 1f) * _shakeMagnitude;
                    float y = Random.Range(-1f, 1f) * _shakeMagnitude;

                    if (_healthUIContainer != null)
                        _healthUIContainer.anchoredPosition = _originalContainerPos + new Vector3(x, y, 0);

                    elapsed += Time.deltaTime;
                }

                bool cancelled = await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
                if (cancelled) return;
            }

            ResetHitFeedbackUI();
        }

        private void ResetHitFeedbackUI()
        {
            if (_healthUIContainer != null) _healthUIContainer.anchoredPosition = _originalContainerPos;
            if (_healthBarFill != null) _healthBarFill.color = _originalHeartColor;
        }
    }
}