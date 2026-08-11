namespace SpinSquad.Core
{
    using System;

    /// <summary>Drives battle idle/walk/attack visuals (line1 sprite rig or Spine skeleton).</summary>
    public interface IBattleVisualDriver
    {
        void PlayAttack(Action onHit, Action onComplete = null);
        void SetMoving(bool isMoving);
        void ForceIdleLoop();
        void UpdateFacing(float dirX);
        void AddMoveDistance(float worldDistance);
    }
}
