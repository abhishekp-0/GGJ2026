public sealed class BallMovementStrategy : IMovementStrategy
{
    public void OnEnter(PlayerMovement ctx)
    {
        ctx.ResetHorizontalMomentum();
        ctx.SetRollVisualActive(true);
        ctx.SetBallBounceActive(true);

        // not rock
        ctx.SetRockSmashActive(false);
        ctx.SetRockMode(false);
    }

    public void OnExit(PlayerMovement ctx)
    {
        ctx.SetRollVisualActive(false);
        ctx.SetBallBounceActive(false);
    }

    public void Tick(PlayerMovement ctx, float dt, bool grounded)
    {
        ctx.MoveHorizontalBallMomentum(dt);

        // bounce decision BEFORE gravity
        ctx.UpdateBallLandingBounce(dt, grounded);

        ctx.ApplyGravityOptimized(dt, grounded);
        ctx.ApplyBallRollVisual(dt);
    }
}
