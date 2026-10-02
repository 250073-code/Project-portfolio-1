using UnityEngine;
using UnityEngine.SceneManagement;

namespace My_Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _victoryPanel;

        [Header("Level Elements")]
        [SerializeField] private GameObject _sniperPrefab;
        [SerializeField] private GameObject _doorPrefab;
        [SerializeField] private GameObject _enemySpawners;

        [Header("Win Condition")]
        [SerializeField] private Health _bossHealth; // Победа наступает, когда у этого Health срабатывает OnDeath

        private Collider _entryTrigger;
        private bool _isGameEnded;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return; // duplicate - don't touch shared state below
            }

            Instance = this;
            _entryTrigger = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            if (_bossHealth != null)
            {
                _bossHealth.OnDeath.AddListener(TriggerVictory);
            }
        }

        private void OnDisable()
        {
            if (_bossHealth != null)
            {
                _bossHealth.OnDeath.RemoveListener(TriggerVictory);
            }
        }

        private void Start()
        {
            SetActiveIfAssigned(_sniperPrefab, true);
            SetActiveIfAssigned(_doorPrefab, false);
            SetActiveIfAssigned(_enemySpawners, false);

            if (_bossHealth == null)
            {
                Debug.LogWarning($"{name}: no boss Health assigned - victory can never trigger.");
            }
        }

        public void TriggerGameOver()
        {
            if (_isGameEnded) return;
            _isGameEnded = true;

            ShowEndPanel(_gameOverPanel);
            EndGameLogic();
        }

        private void TriggerVictory()
        {
            if (_isGameEnded) return;
            _isGameEnded = true;

            ShowEndPanel(_victoryPanel);
            EndGameLogic();
        }

        private void EndGameLogic()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            SetActiveIfAssigned(_sniperPrefab, false);
            SetActiveIfAssigned(_doorPrefab, true);
            SetActiveIfAssigned(_enemySpawners, true);

            WeaponManager.Instance?.UnlockRifle();

            if (_entryTrigger != null)
            {
                _entryTrigger.enabled = false;
            }
        }

        private static void SetActiveIfAssigned(GameObject obj, bool active)
        {
            if (obj != null) obj.SetActive(active);
        }

        // Canvas draws children in hierarchy order: later siblings render (and receive
        // raycasts) ON TOP of earlier ones. Moving the end panel to the last position
        // guarantees no other UI element (e.g. the boss health bar) can cover its buttons.
        private static void ShowEndPanel(GameObject panel)
        {
            if (panel == null) return;

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }
    }
}