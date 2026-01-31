public sealed class BallMovementStrategy : IMovementStrategy
{
    public void OnEnter(PlayerMovement ctx)
    {
        ctx.ClearWallStickState();     // safety if swapping from cube
        ctx.ResetHorizontalMomentum();
        ctx.SetRollVisualActive(true);
    }

    public void OnExit(PlayerMovement ctx)
    {
        ctx.SetRollVisualActive(false);
    }

    public void Tick(PlayerMovement ctx, float dt, bool grounded)
    {
        ctx.MoveHorizontalBallMomentum(dt);
        ctx.ApplyGravityOptimized(dt, grounded);
        ctx.ApplyBallRollVisual(dt);
    }
}
