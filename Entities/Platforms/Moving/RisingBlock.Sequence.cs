using System.Collections;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.HonlyHelper.Entities.Platforms.Moving
{
    public partial class RisingBlock
    {
        internal IEnumerator Rise()
        {
            while (!Triggered && (finalBoss || !PlayerFallCheckCore()))
            {
                yield return null;
            }

            while (FallDelay > 0f)
            {
                FallDelay -= engine.DeltaTime;
                yield return null;
            }

            while (true)
            {
                ShakeSfxCore();
                StartShaking();
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                    RumbleLength.Medium);
                if (finalBoss)
                {
                    OwnedComponents.Add(new SimulationCoroutine(engine, HighlightFadeCore(1f)));
                }

                yield return 0.2f;
                float timer = 0.4f;
                if (finalBoss)
                {
                    timer = 0.2f;
                }

                while (timer > 0f && PlayerWaitCheckCore())
                {
                    yield return null;
                    timer -= engine.DeltaTime;
                }

                StopShaking();
                EmitFallParticlesCore();

                float speed = 0f;
                float maxSpeed = finalBoss ? -130f : -160f;
                while (true)
                {
                    OriginalLevelSceneRoot level = ((OriginalLevelSceneRoot)Scene);
                    speed = ScalarMath.Approach(speed, maxSpeed, 500f * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                    if (MoveVCollideSolids(speed * engine.DeltaTime, thruDashBlocks: true))
                    {
                        break;
                    }

                    if (Top > level.Bounds.Bottom + 16 || (Top > level.Bounds.Bottom - 1 &&
                                                           CheckAt(solidKey, new Vector2(Position.X, Top - 1f))))
                    {
                        Collidable = Visible = false;
                        yield return 0.2f;
                        if (((OriginalChapterMapData)level.Session.MapData).CanTransitionTo(level,
                                new Vector2(Center.X, Bottom + 12f)))
                        {
                            yield return 0.2f;
                            ((OriginalLevelSceneRoot)Scene).Shake();
                            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                                RumbleLength.Medium);
                        }

                        RemoveSelf();
                        DestroyStaticMovers();
                        yield break;
                    }

                    yield return null;
                }

                ImpactSfxCore();
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                    RumbleLength.Medium);
                ((OriginalLevelSceneRoot)Scene).DirectionalShake(Vector2.UnitY, finalBoss ? 0.2f : 0.3f);
                if (finalBoss)
                {
                    OwnedComponents.Add(new SimulationCoroutine(engine, HighlightFadeCore(0f)));
                }

                StartShaking();
                LandParticlesCore();
                yield return 0.2f;
                StopShaking();
                if (CheckAt(tilesKey, new Vector2(Position.X, Top - 1f)))
                {
                    break;
                }

                while (CheckAt(platformKey, new Vector2(Position.X, Top - 1f)))
                {
                    yield return 0.1f;
                }
            }

            Safe = true;
        }

        private bool CheckAt(string key, Vector2 at) => SourceTrackedEntityCollision.CheckAt(
            ((ISourceScene)Scene).SourceScene.Entities, key, this, at);
    }
}
