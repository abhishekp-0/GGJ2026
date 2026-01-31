using UnityEngine;

[CreateAssetMenu(menuName = "GGJ/Jump/Jump Strategy Config", fileName = "JumpStrategy_Config")]
public sealed class JumpStrategy_ConfigSO : ScriptableObject
{
    // Convert jump height (meters) to initial jump velocity
    public float GetJumpVelocity(MaskDefinition.JumpProfile profile, float gravityMagnitude)
    {
        // v = sqrt(2gh)
        return Mathf.Sqrt(2f * gravityMagnitude * Mathf.Max(0.01f, profile.jumpHeight));
    }

    public float GetFallMultiplier(MaskDefinition.JumpProfile profile)
        => Mathf.Max(1f, profile.fallGravityMultiplier);

    public float GetLowJumpMultiplier(MaskDefinition.JumpProfile profile)
        => Mathf.Max(1f, profile.lowJumpGravityMultiplier);

    public bool ShouldBounceOnLanding(MaskDefinition.JumpProfile profile, float lastFrameFallSpeed)
    {
        if (!profile.enableLandingBounce) return false;
        return lastFrameFallSpeed >= profile.bounceMinFallSpeed;
    }

    public float GetBounceVelocity(MaskDefinition.JumpProfile profile)
        => profile.bounceVelocity;

    public float GetCoyoteTime(MaskDefinition.JumpProfile profile)
        => Mathf.Max(0f, profile.coyoteTime);

    public float GetBufferTime(MaskDefinition.JumpProfile profile)
        => Mathf.Max(0f, profile.jumpBufferTime);
}
