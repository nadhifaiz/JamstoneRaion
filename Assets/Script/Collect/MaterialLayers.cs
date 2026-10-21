using UnityEngine;

public static class MaterialLayers
{
    public const int MaxIndex = 2;
    public const int Count = MaxIndex + 1;

    private static readonly string[] LayerNames = { "Material0", "Material1", "Material2" };
    private static readonly int[] unityLayers = new int[Count];
    private static bool initialized;

    public static int ToUnityLayer(int index)
    {
        Initialize();
        return unityLayers[Mathf.Clamp(index, 0, MaxIndex)];
    }

    public static int ToMask(int index) => 1 << ToUnityLayer(index);

    public static int TopMask => ToMask(MaxIndex);

    private static void Initialize()
    {
        if (initialized) return;

        for (int i = 0; i < Count; i++)
        {
            unityLayers[i] = LayerMask.NameToLayer(LayerNames[i]);
            if (unityLayers[i] < 0)
            {
                Debug.LogError($"[MaterialLayers] Layer '{LayerNames[i]}' tidak ada di Project Settings > Tags and Layers.");
            }
        }
        initialized = true;
    }
}