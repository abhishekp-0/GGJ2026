using UnityEngine;

[CreateAssetMenu(menuName = "GGJ/Masks/Mask Library", fileName = "MaskLibrary")]
public sealed class MaskLibrary: ScriptableObject
{
    public MaskDefinition[] masks;

    public MaskDefinition GetByIndex(int index)
    {
        if (masks == null || masks.Length == 0) return null;
        if (index < 0 || index >= masks.Length) return null;
        return masks[index];
    }
}
