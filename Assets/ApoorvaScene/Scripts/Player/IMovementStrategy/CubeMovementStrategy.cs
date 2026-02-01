public sealed class CubeMovementStrategy : IMovementStrategy
{
    public void OnEnter(PlayerMovement ctx)
    {
        ctx.SetRollVisualActive(false);
        ctx.SetBallBounceActive(false);
        ctx.SetRockSmashActive(false);
        ctx.SetRockMode(false);

        ctx.ClearWallStickState();
    }

    public void OnExit(PlayerMovement ctx)
    {
        ctx.ClearWallStickState();
    }

    public void Tick(PlayerMovement ctx, float dt, bool grounded)
    {
        ctx.MoveHorizontalImmediate(dt);

        if (!grounded)
            ctx.HandleWallStick(dt);
        else
            ctx.ClearWallStickState();

        ctx.ApplyGravityOptimized(dt, grounded, allowStickOverride: true);
    }
}
