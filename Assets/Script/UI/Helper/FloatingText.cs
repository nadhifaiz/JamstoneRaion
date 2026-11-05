using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI textMesh;

    [Header("Animation Settings")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float duration = 1f;

    private Color textColor;
    private float elapsedTime;

    private void Awake()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshProUGUI>();

        textColor = textMesh.color;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        // Move upward
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Fade out
        float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);

        textMesh.color = new Color(
            textColor.r,
            textColor.g,
            textColor.b,
            alpha
        );

        if (elapsedTime >= duration)
            Destroy(gameObject);
    }

    public void SetText(string message)
    {
        textMesh.text = message;
    }

    public void SetColor(Color color)
    {
        textColor = color;
        textMesh.color = color;
    }
}