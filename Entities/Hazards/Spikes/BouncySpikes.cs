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
    [SceneTracked(false)]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.HonlyHelper.BouncySpikes")]
    [CustomEntity(
        "HonlyHelper/BouncySpikesUp = BounceUp",
        "HonlyHelper/BouncySpikesDown = BounceDown",
        "HonlyHelper/BouncySpikesLeft = BounceLeft",
        "HonlyHelper/BouncySpikesRight = BounceRight"
    )]
    public partial class BouncySpikes : SceneEntityState, ISourceEngineContextEntity
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ParticleTypePresentation> BounceParticles =
            new();

        private readonly ParticleTypePresentation bounceParticle;
        public ISourceEngineContext EngineContext { get; }

        public Directions Direction;

        private readonly string texture;
        private readonly int size;
        private readonly float particleAngle;
        private readonly bool FreezeFrameEnable = false;

        private bool dashedintoit;
        private bool intoit;
        // The published implementation never assigns this field.
#pragma warning disable CS0649
        private Vector2 speed;
#pragma warning restore CS0649
        private Vector2 particlePosAdjust;
        private Vector2 particlePosAdjustTwo;

        public BouncySpikes(Vector2 position, int size, Directions direction, string texture, bool FreezeFrameEnable,
            ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            EngineContext = engine;
            Position = position;
            bounceParticle = BounceParticles.GetValue(engine, owner =>
                new ParticleTypePresentation(((ISourcePlayerDashParticleEnvironment)owner).DashA));
            Depth = -1;
            Direction = direction;
            this.size = size;
            this.texture = texture;
            this.FreezeFrameEnable = FreezeFrameEnable;

            // making the bounce particles
            bounceParticle.Color = new PackedColor(173, 216, 230);
            bounceParticle.Color2 = new PackedColor(173, 216, 230);
            bounceParticle.FadeMode = ParticleTypePresentation.FadeModes.Late;
            bounceParticle.SpeedMin = 20f;
            bounceParticle.SpeedMax = 25f;
            bounceParticle.LifeMin = 0.5f;
            bounceParticle.LifeMax = 0.7f;

            switch (Direction)
            {
                case Directions.Up:
                    particleAngle = (float)(11 * Math.PI / 8);
                    particlePosAdjust = Vector2.Zero;
                    particlePosAdjustTwo = -Vector2.UnitX;
                    Collider = new HitboxColliderGeometry(size, 8f, 0f, -8f);
                    Add(new LedgeBlockerState());
                    break;
                case Directions.Down:
                    particleAngle = (float)Math.PI * 3f / 8f;
                    particlePosAdjust = new Vector2(0, -10);
                    particlePosAdjustTwo = Vector2.UnitX;
                    Collider = new HitboxColliderGeometry(size, 8f);
                    break;
                case Directions.Left:
                    particleAngle = (float)(7 * Math.PI / 8);
                    particlePosAdjust = new Vector2(5, -8);
                    particlePosAdjustTwo = Vector2.UnitY;
                    Collider = new HitboxColliderGeometry(8f, size, -8f);
                    Add(new LedgeBlockerState());
                    break;
                case Directions.Right:
                    particleAngle = (float)(15 * Math.PI / 8);
                    particlePosAdjust = new Vector2(-4, -8);
                    particlePosAdjustTwo = -Vector2.UnitY;
                    Collider = new HitboxColliderGeometry(8f, size);
                    Add(new LedgeBlockerState());
                    break;
            }

            Add(new PlayerColliderComponentState(OnCollide));
            Add(new HoldableColliderState(OnHoldableCollide));
            Add(new SourceStaticMoverState
            {
                OnShake = OnShake,
                SolidChecker = IsRiding,
                JumpThruChecker = IsRiding
            });
        }

        public BouncySpikes(OriginalLevelEntityData data, Vector2 offset, Directions dir, ISourceEngineContext engine)
            : this(data.Position + offset, GetSize(data, dir), dir,
                data.Attr("texture", "objects/HonlyHelper/BouncySpikes/bouncer"), data.Bool("FreezeFrameEnable"),
                engine)
        {
        }

        public enum Directions
        {
            Up,
            Down,
            Left,
            Right
        }

        public static SceneEntityState BounceUp(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 position, OriginalLevelEntityData entityData)
        {
            return new BouncySpikes(entityData, position, Directions.Up, level.EngineContext);
        }

        public static SceneEntityState BounceDown(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 position, OriginalLevelEntityData entityData)
        {
            return new BouncySpikes(entityData, position, Directions.Down, level.EngineContext);
        }

        public static SceneEntityState BounceLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 position, OriginalLevelEntityData entityData)
        {
            return new BouncySpikes(entityData, position, Directions.Left, level.EngineContext);
        }

        public static SceneEntityState BounceRight(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 position, OriginalLevelEntityData entityData)
        {
            return new BouncySpikes(entityData, position, Directions.Right, level.EngineContext);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            SpikeDecorationBuilder.Add(EngineContext, this, size, (SpikeDirection)Direction, "", _ => texture,
                ((ISourceSpikeScene)scene).SpikeRandom);
        }

        public override void Update()
        {
            base.Update();
            RoomPlayerSession player =
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player != null)
            {
                if (intoit)
                {
                    if (((ISourceVectorEnvironment)EngineContext).VectorArithmetic.Length(player.Data.Motion.Normal
                            .Speed) < ((ISourceVectorEnvironment)EngineContext).VectorArithmetic.Length(speed) - 10)
                    {
                        player.Data.Motion.Normal.Speed = speed;
                    }

                    if (!SourceEntityCollision.Check(this, player))
                    {
                        if (dashedintoit)
                        {
                            player.Machine.State = 0;
                            dashedintoit = false;
                        }

                        if (!player.Inventory.NoRefills)
                        {
                            player.RefillDash();
                        }

                        player.RefillStamina();
                        intoit = false;
                    }
                }
            }
        }

        private static int GetSize(OriginalLevelEntityData data, Directions dir)
        {
            return dir == Directions.Left || dir == Directions.Right ? data.Height : data.Width;
        }

        private void OnShake(Vector2 amount)
        {
            foreach (SourceComponentState component in OwnedComponents)
            {
                if (component is SpriteImage image)
                {
                    image.Position += amount;
                }
            }
        }
    }
}
