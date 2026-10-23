// SPDX-License-Identifier: MIT

using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.HonlyHelper.Entities
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.HonlyHelper.Moth")]
    [CustomEntity("HonlyHelper/Moth")]
    public sealed class Moth : FlyingBug
    {
        private VertexLightState lamp;

        public Moth(ISourceEngineContext engine, Vector2 position) : base(engine, position, 40f, 200f)
        {
            Add(new LightOccludeState(new IntRect(0, 0, 1, 1), .25f));
        }

        public Moth(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset)
        {
        }

        public override void Awake(object scene)
        {
            lamp = GetNearestLight(Position);
            Add(new SimulationCoroutine(Engine, MothUpdate()));
            BugColor = new PackedColor(SourceRandomMath.NextFloat(Random) * .1f + .50f,
                SourceRandomMath.NextFloat(Random) * .10f + .5f,
                SourceRandomMath.NextFloat(Random) * .10f + .5f);
            base.Awake(scene);
        }

        public override void Update()
        {
            FlightGoal = lamp != null
                ? ((ISourceEntityPosition)lamp.Entity).Position + lamp.Position
                : Position - NewSpeed * Engine.DeltaTime * 20f;
            base.Update();
        }

        private IEnumerator MothUpdate()
        {
            while (true)
            {
                lamp = GetNearestLight(Position);
                yield return SourceRandomMath.NextFloat(Random) * .05f + .2f;
            }
        }
    }
}
