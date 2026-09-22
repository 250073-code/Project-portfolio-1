using UnityEngine;

namespace My_Scripts
{
    public class LootDrop : MonoBehaviour
    {
        [SerializeField] private GameObject _medkitPrefab; // Префаб аптечки

        // Вызовем это через UnityEvent OnDeath в твоем скрипте Health
        public void BreakCrate()
        {
            // Спавним аптечку на месте ящика
            if (_medkitPrefab != null)
            {
                Instantiate(_medkitPrefab, transform.position, Quaternion.identity);
            }
        
            // Уничтожаем сам ящик
            Destroy(gameObject);
        }
    }
}