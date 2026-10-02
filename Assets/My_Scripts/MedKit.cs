using UnityEngine;

namespace My_Scripts
{
    public class Medkit : MonoBehaviour
    {
        [SerializeField] private float _healAmount = 25f;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // GetComponentInParent, not GetComponent: Health may live higher up the
            // hierarchy than the collider that actually entered the trigger.
            Health playerHealth = other.GetComponentInParent<Health>();
            if (playerHealth == null) return;

            // Don't let a full-health player waste the pickup - leave it on the ground
            // so they (or someone else) can grab it later when it's actually needed.
            if (playerHealth.CurrentHealth >= playerHealth.MaxHealth) return;

            playerHealth.RestoreHealth(_healAmount);
            Destroy(gameObject);
        }
    }
}