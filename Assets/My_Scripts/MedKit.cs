using UnityEngine;

namespace My_Scripts
{
    public class Medkit : MonoBehaviour
    {
        [SerializeField] private float _healAmount = 25f;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            Health playerHealth = other.GetComponentInParent<Health>();
            if (playerHealth == null) return;

            playerHealth.RestoreHealth(_healAmount);
            Destroy(gameObject);
        }
    }
}