using UnityEngine;

/// <summary>
/// Система выпадения предметов (лута) после смерти.
/// </summary>
public class LootDropper : MonoBehaviour
{
    [System.Serializable]
    public class LootItem
    {
        public GameObject itemPrefab;
        [Range(0, 1)]
        public float dropChance = 0.5f;
        public Vector2Int amountRange = new Vector2Int(1, 1);
    }

    [Header("Loot Settings")]
    [SerializeField] private LootItem[] lootTable;
    [Tooltip("Позиция, откуда будет выпадать лут. Если не задано, используется позиция объекта.")]
    [SerializeField] private Transform dropPoint;
    [Tooltip("Сила, с которой предметы 'выбрасываются' при смерти")]
    [SerializeField] private float dropForce = 2f;

    private void Awake()
    {
        if (dropPoint == null)
        {
            dropPoint = transform;
        }
    }

    /// <summary>
    /// Основной метод для генерации и спавна лута.
    /// Вызывается при смерти животного.
    /// </summary>
    public void DropLoot()
    {
        if (lootTable == null || lootTable.Length == 0)
        {
            return;
        }

        Debug.Log($"Dropping loot for {gameObject.name}...");

        foreach (var lootItem in lootTable)
        {
            // Проверяем шанс выпадения
            if (Random.value <= lootItem.dropChance)
            {
                // Определяем количество предметов для спавна
                int amountToDrop = Random.Range(lootItem.amountRange.x, lootItem.amountRange.y + 1);

                for (int i = 0; i < amountToDrop; i++)
                {
                    SpawnItem(lootItem.itemPrefab);
                }
            }
        }
    }

    private void SpawnItem(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Loot item prefab is not assigned.");
            return;
        }

        // Создаем экземпляр предмета
        GameObject droppedItem = Instantiate(prefab, dropPoint.position, Quaternion.identity);

        // Добавляем небольшую случайную силу, чтобы предметы разлетались
        if (dropForce > 0 && droppedItem.GetComponent<Rigidbody>() != null)
        {
            Vector3 randomDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f), // Предпочитаем 'подбрасывать' вверх
                Random.Range(-1f, 1f)
            ).normalized;

            droppedItem.GetComponent<Rigidbody>().AddForce(randomDirection * dropForce, ForceMode.Impulse);
        }
    }
}
