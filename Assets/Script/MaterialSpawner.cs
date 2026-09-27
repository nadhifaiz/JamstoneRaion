using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Tambahkan ini untuk mempermudah penghitungan layer

[RequireComponent(typeof(BoxCollider2D))]
public class MaterialSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject materialPrefabTemplate;
    public MaterialSO[] availableMaterials;

    public float spawnInterval = 0.5f;
    [Tooltip("Batas maksimal rongsokan UNTUK SETIAP LAYER (Total = max * 3)")]
    public int maxPerLayer = 30;

    private BoxCollider2D spawnArea;
    private Coroutine spawnCoroutine;
    public List<MaterialObject> activeMaterials = new List<MaterialObject>();

    void Awake()
    {
        spawnArea = GetComponent<BoxCollider2D>();
        spawnArea.isTrigger = true;
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null) spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    public void ClearAllMaterials()
    {
        foreach (MaterialObject mat in activeMaterials)
        {
            if (mat != null) Destroy(mat.gameObject);
        }
        activeMaterials.Clear();
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            activeMaterials.RemoveAll(item => item == null);

            int count0 = activeMaterials.Count(m => m.layerIndex == 0);
            int count1 = activeMaterials.Count(m => m.layerIndex == 1);
            int count2 = activeMaterials.Count(m => m.layerIndex == 2);

            if (count0 < maxPerLayer) SpawnAtLayer(0);
            else if (count1 < maxPerLayer) SpawnAtLayer(1);
            else if (count2 < maxPerLayer) SpawnAtLayer(2);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnAtLayer(int targetLayer)
    {
        if (availableMaterials.Length == 0 || materialPrefabTemplate == null) return;

        Vector2 spawnPosition = Vector2.zero;
        bool positionFound = false;

        if (targetLayer == 0)
        {
            Bounds bounds = spawnArea.bounds;
            spawnPosition = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );
            positionFound = true;
        }
        else
        {
            List<MaterialObject> baseCandidates = activeMaterials.FindAll(m => m.layerIndex == targetLayer - 1);
            baseCandidates = baseCandidates.OrderBy(x => Random.value).ToList();

            foreach (MaterialObject cand in baseCandidates)
            {
                int layerMaskToCheck = 1 << (6 + targetLayer);
                Collider2D hit = Physics2D.OverlapPoint(cand.transform.position, layerMaskToCheck);

                if (hit == null)
                {
                    spawnPosition = cand.transform.position;
                    positionFound = true;
                    break;
                }
            }
        }

        if (positionFound)
        {
            Quaternion randomRot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            GameObject newObj = Instantiate(materialPrefabTemplate, spawnPosition, randomRot);

            MaterialObject matObj = newObj.GetComponent<MaterialObject>();
            MaterialSO randomData = availableMaterials[Random.Range(0, availableMaterials.Length)];

            matObj.Initialize(randomData, targetLayer);
            activeMaterials.Add(matObj);
        }
    }
}