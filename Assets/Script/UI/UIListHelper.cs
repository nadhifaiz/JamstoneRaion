using System;
using System.Collections.Generic;
using UnityEngine;

public static class UIListHelper
{
    public static void Populate<T>(Transform parent, GameObject prefab, IEnumerable<T> data, Action<GameObject, T> bind)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(child.gameObject);
        }

        foreach (T item in data)
        {
            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            bind(go, item);
        }
    }
}