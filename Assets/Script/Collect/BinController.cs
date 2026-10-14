using UnityEngine;

public class BinController : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        MaterialObject material = collision.GetComponent<MaterialObject>();
        if (material != null)
        {
            InventoryManager.Instance.AddMaterial(material.materialData);
            Debug.Log($"Berhasil mengumpulkan: {material.materialData.itemName}");
            Destroy(material.gameObject);
        }
    }
}
