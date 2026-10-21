using UnityEngine;

public class BinController : MonoBehaviour
{
    // Mengumpulkan SETIAP MaterialObject yang menyentuh trigger bin
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.TryGetComponent(out MaterialObject material)) return;

        InventoryManager.Instance.AddMaterial(material.MaterialData);
        Destroy(material.gameObject);
    }
}