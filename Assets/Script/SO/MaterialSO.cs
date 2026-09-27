using UnityEngine;

public enum MaterialType
{
    Beban,
    Keras,
    Tajam,
    Lentur,
    Pegangan
}

[CreateAssetMenu(fileName = "NewMaterial", menuName = "Game/Material Data")]
public class MaterialSO : ScriptableObject
{
    public string itemName;
    public Sprite itemSprite;
    public MaterialType materialType;
    public int tier = 1;
}