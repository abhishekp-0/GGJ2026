public sealed class RockMovementStrategy : IMovementStrategy
{
    public void OnEnter(PlayerMovement ctx)
    {
        ctx.SetRollVisualActive(false);
        ctx.SetBallBounceActive(false);

        ctx.SetRockMode(true);
        ctx.SetRockSmashActive(true);

        ctx.ResetHorizontalMomentum(); // rock should not keep ball momentum
    }

    public void OnExit(PlayerMovement ctx)
    {
        ctx.SetRockSmashActive(false);
        ctx.SetRockMode(false);
    }

    public void Tick(PlayerMovement ctx, float dt, bool grounded)
    {
        // Heavy object still moves, but "heaviness" should come from mask.speedMultiplier being low
        ctx.MoveHorizontalImmediate(dt);

        // Smash detection BEFORE gravity finalize
        ctx.UpdateRockSmash(dt, grounded);

        ctx.ApplyGravityOptimized(dt, grounded);
    }
}
