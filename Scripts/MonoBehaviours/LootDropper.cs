using UnityEngine;

/// <summary>
/// Компонент, отвечающий за выбрасывание предметов (лута) после смерти животного.
/// </summary>
public class LootDropper : MonoBehaviour
{
    // Здесь можно будет добавить ссылку на таблицу лута (ScriptableObject)
    // [SerializeField] private LootTable lootTable;

    /// <summary>
    /// Вызывается, когда животное умирает, чтобы выбросить лут.
    /// </summary>
    public void DropLoot()
    {
        // Логика определения и создания лута
        Debug.Log($"{gameObject.name} dropped loot!");

        // Пример:
        // if (lootTable != null)
        // {
        //     GameObject loot = lootTable.GetRandomLoot();
        //     if (loot != null)
        //     {
        //         Instantiate(loot, transform.position, Quaternion.identity);
        //     }
        // }
    }
}
