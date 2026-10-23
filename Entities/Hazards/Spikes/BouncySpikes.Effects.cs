using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Reflection.Interop;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Engine.Actions;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Processes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Player.Scene.Particles;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Graphics.Animation;

namespace Celeste.Mods.HonlyHelper.Entities.Hazards.Spikes
{
    public partial class BouncySpikes
    {
        private void OnCertifiedHit(Vector2 hitposition, RoomPlayerSession player)
        {
            if (FreezeFrameEnable)
            {
                OriginalCelesteFreeze.Freeze(EngineContext, 0.03f);
            }

            if (player.DashAttacking == true)
            {
                player.Data.Motion.DashAttackTimer = 0f;
                // Original DynamicData writes a detached key for this read-only property.
                OnCertifiedDash(hitposition);
            }

            ((IOriginalGameplayProcess)EngineContext).AudioSession.Play("event:/char/badeline/jump_assisted");
            Emit4Particles(hitposition);
        }

        private void OnCertifiedHoldableHit(Vector2 hitposition, HoldableState helded)
        {
            ((IOriginalGameplayProcess)EngineContext).AudioSession.Play("event:/char/badeline/jump_assisted");
            Emit4Particles(hitposition);
        }

        private void OnCertifiedDash(Vector2 hitposition)
        {
            dashedintoit = true;
            ((OriginalLevelSceneRoot)Scene).Displacement.AddBurst(hitposition + particlePosAdjust, 0.4f, 8f, 64f, 0.5f,
                ((ISourceEasingEnvironment)EngineContext).EasingArithmetic.QuadOut,
                ((ISourceEasingEnvironment)EngineContext).EasingArithmetic.QuadOut);
        }

        private void Emit4Particles(Vector2 hitposition)
        {
            ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(bounceParticle,
                hitposition + particlePosAdjust + particlePosAdjustTwo, particleAngle);
            ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(bounceParticle,
                hitposition + particlePosAdjust - particlePosAdjustTwo, particleAngle + (float)(2 * Math.PI / 8));
            ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(bounceParticle,
                hitposition + particlePosAdjust + particlePosAdjustTwo, particleAngle);
            ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(bounceParticle,
                hitposition + particlePosAdjust - particlePosAdjustTwo, particleAngle + (float)(2 * Math.PI / 8));
        }

        private bool IsRiding(SourceSolidEntityState solid)
        {
            return Direction switch
            {
                Directions.Up => CheckOutside(solid, Position + Vector2.UnitY),
                Directions.Down => CheckOutside(solid, Position - Vector2.UnitY),
                Directions.Left => CheckOutside(solid, Position + Vector2.UnitX),
                Directions.Right => CheckOutside(solid, Position - Vector2.UnitX),
                _ => false,
            };
        }

        private bool IsRiding(ISourceJumpThruEntity jumpThru)
        {
            return Direction switch
            {
                Directions.Up => CheckAt(jumpThru, Position + Vector2.UnitY),
                _ => false,
            };
        }

        private bool CheckAt(ISourceEntityCollider other, Vector2 at)
        {
            var previous = Position;
            Position = at;
            bool result = SourceEntityCollision.Check(this, other);
            Position = previous;
            return result;
        }

        private bool CheckOutside(ISourceEntityCollider other, Vector2 at) =>
            !SourceEntityCollision.Check(this, other) && CheckAt(other, at);
    }
}
