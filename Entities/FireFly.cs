// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Mods.HonlyHelper.Runtime;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Graphics.Display.Colors;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.HonlyHelper.Entities
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.HonlyHelper.FireFly")]
    [CustomEntity("HonlyHelper/FireFly")]
    public sealed class FireFly : FlyingBug
    {
        private const float BlinkTimerMax = 1.5f;
        private const float FriendFindRadius = 40f;
        private const float FriendInfluenceRadius = 64f;
        private const float RoamRadius = 24;
        private static readonly PackedColor GlowColor = SourceHexColor.HexToColor("F7F440");
        private static readonly PackedColor NormalColor = new(.05f, .10f, .05f);
        private static readonly ConditionalWeakTable<ISourceEngineContext, FireFlyPopulationState> populations = new();
        private readonly BloomPointState bloom;
        private readonly Vector2 anchor;
        private float ease;
        private Vector2 longtermFlightGoal;
        private bool blinking;
        public float Brightness;
        public float BlinkTimer;
        public bool ControlsSelf;
        public bool BlinksSelf;
        public bool BlinkCancel;

        public FireFly(ISourceEngineContext engine, Vector2 position, bool controlsSelf, bool blinksSelf)
            : base(engine, position, 40f, 100f)
        {
            Add(bloom = new BloomPointState(0f, 8f));
            anchor = position;
            ControlsSelf = controlsSelf;
            BlinksSelf = blinksSelf;
        }

        public FireFly(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset, true, true)
        {
        }

        public static void ResetPopulation(ISourceEngineContext engine) => populations.Remove(engine);

        public override void Awake(object scene)
        {
            var population = populations.GetValue(Engine, _ => new FireFlyPopulationState());
            if (population.Count == 0)
            {
                population.Count = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(FireFly)).Count;
                if (population.Count > 3000)
                    throw new Exception("please don't put over 3000 fireflies in a room, you put down " +
                                        population.Count +
                                        " fireflies, which is a crime, hand in your ahorn license immediately");
            }

            if (ControlsSelf) Add(new SimulationCoroutine(Engine, FireFlyUpdate()));
            BlinkTimer = BlinkTimerMax * (SourceRandomMath.NextFloat(Random) * .5f + .9f);
            BugColor = NormalColor;
            base.Awake(scene);
        }

        public override void Update()
        {
            BlinkTimer -= Engine.DeltaTime;
            if (BlinkTimer < 0 && !blinking)
            {
                blinking = true;
                Add(new SimulationCoroutine(Engine, Blink()));
            }

            BugColor = PackedColor.Lerp(NormalColor, GlowColor, Brightness);
            bloom.Alpha = Brightness;
            base.Update();
        }

        public IEnumerator Blink()
        {
            var easing = ((ISourceEasingEnvironment)Engine).EasingArithmetic;
            while (Brightness < .98f && !BlinkCancel)
            {
                ease = ScalarMath.Approach(ease, 1f, Engine.DeltaTime * 8f);
                Brightness = easing.CubeIn(ease);
                yield return null;
            }

            MakeSelfSeen();
            while (Brightness > .02 && !BlinkCancel)
            {
                ease = ScalarMath.Approach(ease, 0f, Engine.DeltaTime);
                Brightness = easing.QuadIn(ease);
                yield return null;
            }

            blinking = false;
            BlinkTimer = BlinkTimerMax * (SourceRandomMath.NextFloat(Random) * .5f + .9f);
            BlinkCancel = false;
            yield return null;
        }

        private IEnumerator FireFlyUpdate()
        {
            while (true)
            {
                longtermFlightGoal = anchor + new Vector2((2 * SourceRandomMath.NextFloat(Random) - 1) * RoamRadius,
                    (2 * SourceRandomMath.NextFloat(Random) - 1) * RoamRadius);
                while (Vectors.LengthSquared(longtermFlightGoal - Position) > 5)
                {
                    FlightGoal = Vectors.Approach(FlightGoal, longtermFlightGoal, 10f * Engine.DeltaTime);
                    yield return null;
                }

                yield return SourceRandomMath.NextFloat(Random) * .5f + 1f;
            }
        }

        private void SawBlink(float distance)
        {
            if (BlinksSelf) BlinkTimer *= distance / ((float)Math.Pow(FriendInfluenceRadius, 2) + distance);
        }

        private void MakeSelfSeen()
        {
            if (!BlinksSelf) return;
            var radiusSquared = (float)Math.Pow(FriendFindRadius, 2);
            var flies = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(FireFly))
                .Cast<FireFly>().Where(fly => Vectors.DistanceSquared(Position, fly.Position) < radiusSquared).ToList();
            foreach (var fly in flies)
            {
                if (fly == null) break;
                fly.SawBlink(Vectors.DistanceSquared(Position, fly.Position));
            }
        }
    }
}
