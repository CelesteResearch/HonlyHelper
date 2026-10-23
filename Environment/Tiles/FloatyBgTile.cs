// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.FloatySpaceBlocks;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.HonlyHelper.Environment.Tiles
{
    [CustomEntity("HonlyHelper/FloatyBgTile")]
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.HonlyHelper.FloatyBgTile")]
    public sealed class FloatyBgTile : SourcePlatformEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly char tileType;
        private TileGrid tiles;
        private float yLerp;
        private float sinkTimer = .3f;
        private float sineWave;
        private FloatyBgTile master;
        private bool awake;
        internal bool HookedToForeground;
        public List<FloatyBgTile> Group;
        public Dictionary<SourcePlatformEntityState, Vector2> Moves;
        public IntPoint GroupBoundsMin;
        public IntPoint GroupBoundsMax;
        public bool HasGroup { get; private set; }
        public bool MasterOfGroup { get; private set; }
        internal bool IsAwake => awake;
        internal FloatyBgTile Master => MasterOfGroup ? this : master;

        public FloatyBgTile(Vector2 position, float width, float height, char tileType, bool disableSpawnOffset,
            ISourceSolidWorld world) : base(position, true, world)
        {
            engine = world.EngineContext;
            this.tileType = tileType;
            Depth = 10000;
            sineWave = disableSpawnOffset
                ? 0f
                : SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, (float)Math.PI * 2f);
            Collider = new HitboxColliderGeometry(width, height);
        }

        public FloatyBgTile(EntityData data, Vector2 offset, ISourceSolidWorld world)
            : this(data.Position + offset, data.Width, data.Height, data.Char("tiletype", '3'),
                data.Bool("disableSpawnOffset"), world)
        {
        }

        public static FloatyBgTile Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(data, offset,
            ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, true));

        public override void Awake(object scene)
        {
            base.Awake(scene);
            awake = true;
            if (!HasGroup)
            {
                MasterOfGroup = true;
                Moves = new();
                Group = new();
                GroupBoundsMin = new IntPoint((int)X, (int)Y);
                GroupBoundsMax = new IntPoint((int)Right, (int)Bottom);
                AddToGroupAndFindChildren(this);
                var bounds = new IntRect(GroupBoundsMin.X / 8, GroupBoundsMin.Y / 8,
                    (GroupBoundsMax.X - GroupBoundsMin.X) / 8 + 1, (GroupBoundsMax.Y - GroupBoundsMin.Y) / 8 + 1);
                var map = new VirtualMap<char>(bounds.Width, bounds.Height, '0');
                foreach (FloatyBgTile member in Group)
                {
                    int x = (int)(member.X / 8f) - bounds.X;
                    int y = (int)(member.Y / 8f) - bounds.Y;
                    int width = (int)(member.Width / 8f);
                    int height = (int)(member.Height / 8f);
                    for (int i = x; i < x + width; i++)
                    for (int j = y; j < y + height; j++)
                        map[i, j] = member.tileType;
                }

                tiles = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                    .BackgroundAutotiler.GenerateMap(map, new AutotileBehavior
                    {
                        EdgesExtend = false, EdgesIgnoreOutOfLevel = false, PaddingIgnoreOutOfLevel = false
                    }).TileGrid;
                tiles.Position = new Vector2(GroupBoundsMin.X - X, GroupBoundsMin.Y - Y);
                tiles.ClipCamera = ((OriginalLevelSceneRoot)Scene).Camera;
                tiles.VisualExtend = 1;
                Add(tiles);
            }

            TryToInitPosition();
        }

        public override void Update()
        {
            base.Update();
            if (!MasterOfGroup) return;
            if (sinkTimer > 0f) sinkTimer -= engine.DeltaTime;
            yLerp = ScalarMath.Approach(yLerp, sinkTimer > 0f ? 1f : 0f, engine.DeltaTime,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            sineWave += engine.DeltaTime;
            MoveToTarget();
        }

        public override void MoveHExact(int move) => Position += new Vector2(move, 0f);
        public override void MoveVExact(int move) => Position += new Vector2(0f, move);

        public override void Removed(object scene)
        {
            tiles = null;
            Moves = null;
            Group = null;
            base.Removed(scene);
        }

        private void TryToInitPosition()
        {
            if (!MasterOfGroup)
            {
                master.TryToInitPosition();
                return;
            }

            foreach (FloatyBgTile member in Group)
                if (!member.awake)
                    return;
            MoveToTarget();
        }

        private void AddToGroupAndFindChildren(FloatyBgTile from)
        {
            GroupBoundsMin = new IntPoint(Math.Min(GroupBoundsMin.X, (int)from.X),
                Math.Min(GroupBoundsMin.Y, (int)from.Y));
            GroupBoundsMax = new IntPoint(Math.Max(GroupBoundsMax.X, (int)from.Right),
                Math.Max(GroupBoundsMax.Y, (int)from.Bottom));
            from.HasGroup = true;
            Group.Add(from);
            Moves.Add(from, from.Position);
            if (from != this) from.master = this;
            foreach (FloatyBgTile entity in ((OriginalLevelSceneRoot)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(FloatyBgTile)))
            {
                if (entity.HasGroup || !(SourceSceneCollision.CheckRect(new IntRect((int)from.X - 1, (int)from.Y,
                                             (int)from.Width + 2, (int)from.Height), entity)
                                         || SourceSceneCollision.CheckRect(new IntRect((int)from.X, (int)from.Y - 1,
                                             (int)from.Width, (int)from.Height + 2), entity))) continue;
                if (from.HookedToForeground) entity.HookedToForeground = true;
                AddToGroupAndFindChildren(entity);
            }
        }

        private void MoveToTarget()
        {
            if (HookedToForeground) return;
            float sine = (float)Math.Sin(sineWave) * 4f;
            float easing = ((ISourceEasingEnvironment)engine).EasingArithmetic.SineInOut(yLerp);
            foreach (var move in Moves)
                move.Key.Position = new Vector2(move.Value.X,
                    move.Value.Y + (move.Value.Y + 12f - move.Value.Y) * easing + sine);
        }
    }
}
