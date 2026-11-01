using UnityEngine;

public static class UIZoom
{
    public static Vector3 GetWorldCenter(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    // restPosition = localPosition target saat belum di-zoom
    public static void Apply(RectTransform target, Vector3 restPosition, float scale, Vector3 focusWorld)
    {
        RectTransform parent = target.parent as RectTransform;
        Vector3 focusLocal = parent != null ? parent.InverseTransformPoint(focusWorld) : focusWorld;

        target.localScale = new Vector3(scale, scale, 1f);
        target.localPosition = restPosition + (1f - scale) * (focusLocal - restPosition);
    }

    public static void Reset(RectTransform target, Vector3 restPosition)
    {
        target.localScale = Vector3.one;
        target.localPosition = restPosition;
    }
}