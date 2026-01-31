using UnityEngine;

public sealed class CubeMovementStrategy : IMovementStrategy
{
    public void OnEnter(PlayerMovement ctx)
    {
        ctx.SetRollVisualActive(false);
        ctx.ClearWallStickState();
    }

    public void OnExit(PlayerMovement ctx)
    {
        ctx.ClearWallStickState();
    }

    public void Tick(PlayerMovement ctx, float dt, bool grounded)
    {
        ctx.MoveHorizontalImmediate(dt);

        if (grounded)
        {
            ctx.ClearWallStickState();
        }
        else
        {
            // ✅ pass dt (even if not used inside)
            ctx.HandleWallStick(dt);
        }

        ctx.ApplyGravityOptimized(dt, grounded, allowStickOverride: true);
    }
}
