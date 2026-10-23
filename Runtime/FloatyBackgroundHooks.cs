// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.FloatySpaceBlocks;
using Celeste.Mods.HonlyHelper.Environment.Tiles;
using Celeste.Scene.Entities;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.HonlyHelper.Runtime
{
    public sealed class FloatyBackgroundHooks : IDisposable
    {
        private static readonly MethodInfo addToGroup = typeof(FloatySpaceBlockState)
            .GetMethod("AddToGroupAndFindChildren", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Action<FloatySpaceBlockState, FloatySpaceBlockState> invokeAddToGroup =
            (Action<FloatySpaceBlockState, FloatySpaceBlockState>)addToGroup.CreateDelegate(
                typeof(Action<FloatySpaceBlockState, FloatySpaceBlockState>));

        private readonly ISourceEngineContext engine;
        private readonly ConditionalWeakTable<FloatySpaceBlockState, List<FloatyBgTile>> backgroundGroups = new();
        private readonly List<IDisposable> hooks = new();

        public FloatyBackgroundHooks(ISourceEngineContext engine) => this.engine = engine;

        public void Load()
        {
            try
            {
                hooks.Add(new Hook(addToGroup,
                    (Action<Action<FloatySpaceBlockState, FloatySpaceBlockState>, FloatySpaceBlockState,
                        FloatySpaceBlockState>)AddToGroup));
                hooks.Add(new Hook(typeof(FloatySpaceBlockState).GetMethod(nameof(FloatySpaceBlockState.Awake)),
                    (Action<Action<FloatySpaceBlockState, object>, FloatySpaceBlockState, object>)Awake));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            backgroundGroups.Clear();
        }

        private bool Owns(FloatySpaceBlockState block) => block.Scene is OriginalLevelSceneRoot level
                                                          && ReferenceEquals(level.EngineContext, engine);

        private void Awake(Action<FloatySpaceBlockState, object> orig, FloatySpaceBlockState self, object scene)
        {
            if (Owns(self) && !self.HasGroup)
            {
                backgroundGroups.Remove(self);
                backgroundGroups.Add(self, new List<FloatyBgTile>());
            }

            orig(self, scene);
        }

        private void AddToGroup(Action<FloatySpaceBlockState, FloatySpaceBlockState> orig, FloatySpaceBlockState self,
            FloatySpaceBlockState from)
        {
            orig(self, from);
            if (!Owns(self) || !backgroundGroups.TryGetValue(self, out List<FloatyBgTile> list)) return;
            var scene = (OriginalLevelSceneRoot)self.Scene;
            var rectangle = new IntRect((int)from.X, (int)from.Y, (int)from.Width, (int)from.Height);
            // Source CollideAll snapshots the matches before recursively discovering more foreground children.
            var matches = new List<FloatyBgTile>();
            foreach (FloatyBgTile tile in scene.SourceScene.Entities.GetTrackedEntities(typeof(FloatyBgTile)))
                if (SourceSceneCollision.CheckRect(rectangle, tile))
                    matches.Add(tile);
            foreach (FloatyBgTile tile in matches)
            {
                if (list.Contains(tile)) continue;
                if (!tile.IsAwake) tile.Awake(self.Scene);
                list.Add(tile);
                self.Moves[tile] = tile.Position;
                FloatyBgTile master = tile.Master;
                master.HookedToForeground = true;
                foreach (FloatyBgTile member in master.Group)
                {
                    self.Moves[member] = member.Position;
                    var bounds = new IntRect((int)member.X, (int)member.Y, (int)member.Width, (int)member.Height);
                    foreach (FloatySpaceBlockState child in scene.SourceScene.Entities.GetTrackedEntities(
                                 "Celeste.FloatySpaceBlock"))
                        if (!child.HasGroup && SourceSceneCollision.CheckRect(bounds, child))
                            invokeAddToGroup(self, child);
                }
            }
        }
    }
}
