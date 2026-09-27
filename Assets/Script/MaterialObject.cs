using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class MaterialObject : MonoBehaviour
{
    public MaterialSO materialData;
    public int layerIndex = 0;

    // Inisialisasi data material.
    public void Initialize(MaterialSO data, int assignedLayer)
    {
        materialData = data;
        layerIndex = assignedLayer;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = materialData.itemSprite;
        sr.sortingOrder = layerIndex;

        gameObject.layer = 6 + layerIndex;

        if (layerIndex == 0) sr.color = new Color(0.4f, 0.4f, 0.4f);
        else if (layerIndex == 1) sr.color = new Color(0.7f, 0.7f, 0.7f);
        else sr.color = Color.white;
    }

    // Menaikan layer
    public void PromoteLayer()
    {
        if (layerIndex >= 2) return;

        Initialize(this.materialData, layerIndex + 1);
    }

    // Menurunkan layer
    public void DemoteLayer()
    {
        if (transform.parent != null) return;

        if (layerIndex == 0)
        {
            Destroy(gameObject);
        }
        else
        {
            Initialize(this.materialData, layerIndex - 1);
            CrushUnderneath();
        }
    }

    // Memaksa material di bawahnya untuk turun layer setalah material ini diturunkan
    private void CrushUnderneath()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (Collider2D hit in hits)
        {
            MaterialObject underMat = hit.GetComponent<MaterialObject>();

            if (underMat != null && underMat != this && underMat.layerIndex == this.layerIndex)
            {
                underMat.DemoteLayer();
            }
        }
    }
}