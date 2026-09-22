using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace My_Scripts
{
    public class Grenade : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [SerializeField] private float _delay = 3f;
        [SerializeField] private float _explosionRadius = 5f;
        [SerializeField] private float _explosionForce = 700f;
        [SerializeField] private float _damageAmount = 50f;
        [SerializeField] private LayerMask _damageableLayers = ~0; // restrict what OverlapSphere considers

        [Header("Visual Effects")]
        [SerializeField] private GameObject _explosionEffectPrefab;
        [SerializeField] private float _explosionEffectLifetime = 2f;

        private void Start()
        {
            ExplodeAfterDelay().Forget();
        }

        private async UniTaskVoid ExplodeAfterDelay()
        {
            await UniTask.WaitForSeconds(_delay, cancellationToken: this.GetCancellationTokenOnDestroy());
            Explode();
        }

        private void Explode()
        {
            SpawnExplosionEffect();
            DamageAndPushNearbyObjects();
            Destroy(gameObject);
        }

        private void SpawnExplosionEffect()
        {
            if (_explosionEffectPrefab == null) return;

            GameObject explosionEffect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(explosionEffect, _explosionEffectLifetime);
        }

        private void DamageAndPushNearbyObjects()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRadius, _damageableLayers);
            var damagedObjects = new HashSet<IDamageable>();

            foreach (Collider nearbyObject in colliders)
            {
                if (nearbyObject.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddExplosionForce(_explosionForce, transform.position, _explosionRadius);
                }

                IDamageable damageable = nearbyObject.GetComponentInParent<IDamageable>();
                if (damageable != null && damagedObjects.Add(damageable))
                {
                    damageable.TakeDamage(_damageAmount);
                }
            }
        }
    }
}