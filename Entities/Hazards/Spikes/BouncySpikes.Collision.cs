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
        private void OnCollide(RoomPlayerSession player)
        {
            // Pretty sure this is unintentionally bugged due to short-circuiting, but the climb check could break existing maps if changed now? - Kahuna
            if (!(player.Machine.State == 21 || player.Machine.State == 16 || player.Machine.State == 1))
            {
                switch (Direction)
                {
                    case Directions.Up:
                        if (player.Data.Motion.Normal.Speed.Y >= 0f && player.Bottom <= Bottom)
                        {
                            if (player.DashAttacking)
                            {
                                player.Data.Motion.Normal.Speed.Y = player.Data.Direction.X != 0 ? -260f : -320f;
                                player.Data.Direction.Y = -player.Data.Direction.Y;
                            }
                            else
                            {
                                player.Data.Motion.Normal.Speed.Y = player.Data.Motion.Normal.Speed.Y < 150f
                                    ? -180f
                                    : -1.2f * player.Data.Motion.Normal.Speed.Y;
                            }

                            OnCertifiedHit(player.Position, player);
                            intoit = true;
                        }

                        break;
                    case Directions.Down:
                        if (player.Data.Motion.Normal.Speed.Y <= 0f)
                        {
                            player.Data.Motion.Normal.Speed.Y = player.Data.Motion.Normal.Speed.Y > -75f
                                ? 90f
                                : -1.2f * player.Data.Motion.Normal.Speed.Y;
                            if (player.DashAttacking)
                            {
                                player.Data.Direction.Y = -player.Data.Direction.Y;
                            }

                            OnCertifiedHit(player.Position, player);
                            intoit = true;
                        }

                        break;
                    case Directions.Left:
                        if (player.Data.Motion.Normal.Speed.X >= 0f)
                        {
                            if (player.DashAttacking && player.Data.Direction.X != 0)
                            {
                                if (player.Data.Direction.Y < 0)
                                {
                                    player.Data.Motion.Normal.Speed.X = -1.4f * player.Data.Motion.Normal.Speed.X;
                                    player.Data.Motion.Normal.Speed.Y = 1.4f * player.Data.Motion.Normal.Speed.Y;
                                    if (player.Data.Motion.Normal.Speed.Y < 169)
                                    {
                                        player.Data.Motion.Normal.Speed.Y = -237;
                                    }
                                }
                                else
                                {
                                    player.Data.Motion.Normal.Speed.X = player.Data.Direction.Y > 0
                                        ? -1.4f * player.Data.Motion.Normal.Speed.X
                                        : -player.Data.Motion.Normal.Speed.X;
                                }

                                player.Data.Direction.X = -player.Data.Direction.X;
                                if (player.Data.Motion.Normal.Speed.X > -168)
                                {
                                    player.Data.Motion.Normal.Speed.X = -240;
                                }
                            }
                            else
                            {
                                player.Data.Motion.Normal.Speed.X = player.Data.Motion.Normal.Speed.X < 240f
                                    ? -240f
                                    : -1.2f * player.Data.Motion.Normal.Speed.X;
                            }

                            OnCertifiedHit(player.Position, player);
                            intoit = true;
                        }

                        break;
                    case Directions.Right:
                        if (player.Data.Motion.Normal.Speed.X <= 0f)
                        {
                            if (player.DashAttacking && player.Data.Direction.X != 0)
                            {
                                if (player.Data.Direction.Y < 0)
                                {
                                    player.Data.Motion.Normal.Speed.X = -1.4f * player.Data.Motion.Normal.Speed.X;
                                    player.Data.Motion.Normal.Speed.Y = 1.4f * player.Data.Motion.Normal.Speed.Y;
                                    if (player.Data.Motion.Normal.Speed.Y < 169)
                                    {
                                        player.Data.Motion.Normal.Speed.Y = -237;
                                    }
                                }
                                else
                                {
                                    player.Data.Motion.Normal.Speed.X = player.Data.Direction.Y > 0
                                        ? -1.4f * player.Data.Motion.Normal.Speed.X
                                        : -player.Data.Motion.Normal.Speed.X;
                                }

                                player.Data.Direction.X = -player.Data.Direction.X;
                                if (player.Data.Motion.Normal.Speed.X < 168)
                                {
                                    player.Data.Motion.Normal.Speed.X = 240;
                                }
                            }
                            else
                            {
                                player.Data.Motion.Normal.Speed.X = player.Data.Motion.Normal.Speed.X > -240f
                                    ? 240f
                                    : -1.2f * player.Data.Motion.Normal.Speed.X;
                            }

                            OnCertifiedHit(player.Position, player);
                            intoit = true;
                        }

                        break;
                }
            }
        }

        private void OnHoldableCollide(HoldableState holded)
        {
            var dyn = BouncyHoldableSpeed.For(EngineContext, (SceneEntityState)holded.Entity);
            if (dyn.TryGet("Speed", out Vector2 theSpeed))
            {
                switch (Direction)
                {
                    case Directions.Up:
                        if (theSpeed.Y >= 0f)
                        {
                            if (theSpeed.Y < 150f)
                            {
                                dyn.Set("Speed", new Vector2(theSpeed.X, -180f));
                            }
                            else
                            {
                                dyn.Set("Speed", new Vector2(theSpeed.X, -1.2f * theSpeed.Y));
                            }

                            OnCertifiedHoldableHit(((SceneEntityState)holded.Entity).Position, holded);
                        }

                        break;
                    case Directions.Down:
                        if (theSpeed.Y <= 0f)
                        {
                            if (theSpeed.Y > -75f)
                            {
                                dyn.Set("Speed", new Vector2(theSpeed.X, 90f));
                            }
                            else
                            {
                                dyn.Set("Speed", new Vector2(theSpeed.X, -1.2f * theSpeed.Y));
                            }

                            OnCertifiedHoldableHit(((SceneEntityState)holded.Entity).Position, holded);
                        }

                        break;
                    case Directions.Left:
                        if (theSpeed.X >= 0f)
                        {
                            if (theSpeed.X < 240f)
                            {
                                dyn.Set("Speed", new Vector2(-240f, theSpeed.Y));
                            }
                            else
                            {
                                dyn.Set("Speed", new Vector2(-1.2f * theSpeed.X, theSpeed.Y));
                            }

                            OnCertifiedHoldableHit(((SceneEntityState)holded.Entity).Position, holded);
                        }

                        break;
                    case Directions.Right:
                        if (theSpeed.X <= 0f)
                        {
                            if (theSpeed.X > -240f)
                            {
                                dyn.Set("Speed", new Vector2(240f, theSpeed.Y));
                            }
                            else
                            {
                                dyn.Set("Speed", new Vector2(-1.2f * theSpeed.X, theSpeed.Y));
                            }

                            OnCertifiedHoldableHit(((SceneEntityState)holded.Entity).Position, holded);
                        }

                        break;
                }
            }
        }
    }
}
