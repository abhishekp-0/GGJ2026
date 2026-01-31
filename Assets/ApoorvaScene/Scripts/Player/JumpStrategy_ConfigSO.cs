using UnityEngine;

[CreateAssetMenu(menuName = "GGJ/Jump/Jump Strategy Config", fileName = "JumpStrategy_Config")]
public sealed class JumpStrategy_ConfigSO : ScriptableObject
{
    public float GetJumpVelocity(MaskDefinition.JumpProfile profile, float gravityMagnitude)
    {
        return Mathf.Sqrt(2f * gravityMagnitude * Mathf.Max(0.01f, profile.jumpHeight));
    }

    public float GetFallMultiplier(MaskDefinition.JumpProfile profile)
        => Mathf.Max(1f, profile.fallGravityMultiplier);

    public float GetLowJumpMultiplier(MaskDefinition.JumpProfile profile)
        => Mathf.Max(1f, profile.lowJumpGravityMultiplier);

    public float GetCoyoteTime(MaskDefinition.JumpProfile profile)
        => Mathf.Max(0f, profile.coyoteTime);

    public float GetBufferTime(MaskDefinition.JumpProfile profile)
        => Mathf.Max(0f, profile.jumpBufferTime);
}
