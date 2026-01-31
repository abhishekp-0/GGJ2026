public sealed class DefaultMovementStrategy : IMovementStrategy
{
    public void OnEnter(PlayerMovement ctx)
    {
        ctx.SetRollVisualActive(false);
        ctx.ClearWallStickState(); // safety if swapping from cube
    }

    public void OnExit(PlayerMovement ctx) { }

    public void Tick(PlayerMovement ctx, float dt, bool grounded)
    {
        ctx.MoveHorizontalImmediate(dt);
        ctx.ApplyGravityOptimized(dt, grounded);
    }
}
