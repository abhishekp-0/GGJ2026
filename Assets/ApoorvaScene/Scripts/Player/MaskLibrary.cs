using UnityEngine;

[CreateAssetMenu(menuName = "GGJ/Masks/Mask Library", fileName = "MaskLibrary")]
public sealed class MaskLibrary : ScriptableObject
{
    public MaskDefinition[] masks;

    public MaskDefinition GetByIndex(int index)
    {
        if (masks == null || masks.Length == 0) return null;
        index = Mathf.Clamp(index, 0, masks.Length - 1);
        return masks[index];
    }

    public MaskDefinition GetById(MaskId id)
    {
        if (masks == null) return null;
        for (int i = 0; i < masks.Length; i++)
            if (masks[i] != null && masks[i].id == id)
                return masks[i];
        return null;
    }
}
