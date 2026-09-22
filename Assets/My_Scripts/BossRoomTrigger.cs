using UnityEngine;

namespace My_Scripts
{
    public class BossRoomTrigger : MonoBehaviour
    {
        [Header("Boss Reference")]
        [SerializeField] private Health _bossHealth;

        private Collider _triggerCollider;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _bossHealth?.ShowBossHealthBar();

            // Disable so the health bar doesn't try to re-trigger every time
            // the player steps back into this zone.
            if (_triggerCollider != null)
            {
                _triggerCollider.enabled = false;
            }
        }
    }
}