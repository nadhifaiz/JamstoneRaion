using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(BoxCollider2D))]
public class MaterialSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject materialPrefabTemplate;
    public MaterialSO[] availableMaterials;

    public float spawnInterval = 0.5f;
    [Tooltip("Batas maksimal rongsokan UNTUK SETIAP LAYER (Total = max * 3)")]
    public int maxPerLayer = 30;

    [Header("Probability Curves (X = Hari, Y = Bobot)")]
    [Tooltip("Bobot relatif, tidak harus berjumlah 100: dinormalisasi otomatis. Setelah keyframe terakhir, nilainya bertahan.")]
    public AnimationCurve tier1Curve = new AnimationCurve(new Keyframe(1, 68), new Keyframe(7, 10));
    public AnimationCurve tier2Curve = new AnimationCurve(new Keyframe(1, 27), new Keyframe(4, 40), new Keyframe(7, 25));
    public AnimationCurve tier3Curve = new AnimationCurve(new Keyframe(1, 5), new Keyframe(7, 65));

    private BoxCollider2D spawnArea;
    private Coroutine spawnCoroutine;
    public List<MaterialObject> activeMaterials = new List<MaterialObject>();

    private Dictionary<int, List<MaterialSO>> materialsByTier;
    private readonly HashSet<int> warnedTiers = new HashSet<int>(); // peringatan tier kosong cukup sekali

    void Awake()
    {
        spawnArea = GetComponent<BoxCollider2D>();
        spawnArea.isTrigger = true;
        BuildTierCache();
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

        // 1. Tentukan posisi spawn (logika tumpukan layer)
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

        // 2. Jika posisi valid, pilih material berdasarkan hari
        if (positionFound)
        {
            Quaternion randomRot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            GameObject newObj = Instantiate(materialPrefabTemplate, spawnPosition, randomRot);

            MaterialObject matObj = newObj.GetComponent<MaterialObject>();
            MaterialSO selectedData = GetRandomMaterialByDay();

            matObj.Initialize(selectedData, targetLayer);
            activeMaterials.Add(matObj);
        }
    }

    // ==========================================
    // GACHA TIER BERDASARKAN HARI
    // ==========================================

    private void BuildTierCache()
    {
        materialsByTier = availableMaterials
            .Where(m => m != null)
            .GroupBy(m => m.tier)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private int GetCurrentDay()
    {
        return GameManager.Instance != null ? GameManager.Instance.currentDay : 1;
    }

    // Bobot tier 1..3 pada hari tertentu. Dijaga >= 0 karena kurva yang diedit bisa overshoot ke negatif.
    private float[] GetTierWeights(int day)
    {
        return new float[]
        {
            Mathf.Max(0f, tier1Curve.Evaluate(day)),
            Mathf.Max(0f, tier2Curve.Evaluate(day)),
            Mathf.Max(0f, tier3Curve.Evaluate(day))
        };
    }

    // Mengembalikan tier 1..3, atau 0 kalau semua bobot nol
    private int RollTier(float[] weights)
    {
        float total = weights[0] + weights[1] + weights[2];
        if (total <= 0f) return 0;

        float roll = Random.Range(0f, total);
        if (roll < weights[0]) return 1;
        if (roll < weights[0] + weights[1]) return 2;
        return 3;
    }

    private MaterialSO GetRandomMaterialByDay()
    {
        int tier = RollTier(GetTierWeights(GetCurrentDay()));

        if (tier != 0 && materialsByTier.TryGetValue(tier, out List<MaterialSO> matching) && matching.Count > 0)
        {
            return matching[Random.Range(0, matching.Count)];
        }

        // Failsafe: tier terpilih belum punya material, ambil acak dari semua
        if (tier != 0 && warnedTiers.Add(tier))
        {
            Debug.LogWarning($"[MaterialSpawner] Tidak ada material Tier {tier} di availableMaterials. Memakai acak dari semua material.");
        }
        return availableMaterials[Random.Range(0, availableMaterials.Length)];
    }

    // Klik kanan komponen di inspector untuk memeriksa persentase hasil kurva
    [ContextMenu("Print Probabilitas Tier (Hari 1-30)")]
    private void PrintTierProbabilities()
    {
        for (int day = 1; day <= 30; day++)
        {
            float[] w = GetTierWeights(day);
            float total = w[0] + w[1] + w[2];

            if (total <= 0f)
            {
                Debug.Log($"Hari {day}: semua bobot 0");
                continue;
            }

            Debug.Log($"Hari {day}: T1 {w[0] / total * 100f:F1}% | T2 {w[1] / total * 100f:F1}% | T3 {w[2] / total * 100f:F1}%");
        }
    }
}