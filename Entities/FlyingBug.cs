// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.HonlyHelper.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.HonlyHelper.FlyingBug")]
    public class FlyingBug : SourceActorEntityState
    {
        protected readonly ISourceEngineContext Engine;
        protected ISourceVectorArithmetic Vectors => ((ISourceVectorEnvironment)Engine).VectorArithmetic;
        protected ISourceRandom Random => ((ISourceRandomEnvironment)Engine).Random;
        public float FlightStrength;
        public float MaxSpeed;
        public PackedColor BugColor = PackedColor.Red;
        public Vector2 Speed = Vector2.Zero;
        public Vector2 OutdatedSpeed = Vector2.Zero;
        public Vector2 NewSpeed = Vector2.Zero;
        public Vector2 Acceleration = Vector2.Zero;
        public Vector2 FlightGoal;
        public float FlyError;

        public FlyingBug(ISourceEngineContext engine, Vector2 position, float flightStrength, float maxSpeed)
            : base(new PixelActor(null, position), engine,
                ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            Engine = engine;
            FlightStrength = flightStrength;
            MaxSpeed = (float)Math.Pow(maxSpeed, 2);
            Depth = -20000;
        }

        public override void Awake(object scene)
        {
            FlightGoal = Position;
            Add(new SimulationCoroutine(Engine, FlyUpdate()));
            base.Awake(scene);
        }

        public override void Update()
        {
            base.Update();
            FlyTowardsGoal(FlightGoal);
        }

        public override void Render(ISourceRenderContext context)
        {
            SourcePrimitiveDrawing.Point(context, this, Position, BugColor);
            base.Render(context);
        }

        public virtual void FlyTowardsGoal(Vector2 goal)
        {
            Vector2 toGoal = goal - Position;
            float speedAngle = Vector2Geometry.Angle(Speed);
            Vector2 speedGoal = Vector2Geometry.AngleToVector(Vector2Geometry.Angle(toGoal), FlightStrength);
            Acceleration = speedGoal - Speed;
            float accelerationLength = Vectors.Length(Acceleration);
            accelerationLength = Math.Abs(accelerationLength) > FlightStrength ? FlightStrength : accelerationLength;
            float angle = speedAngle - Vector2Geometry.Angle(Acceleration);
            float partWithLength = (float)Math.Cos(angle);
            partWithLength = partWithLength > 0f
                ? partWithLength * (1 - Vectors.LengthSquared(Speed) / MaxSpeed)
                : partWithLength * (1 + Vectors.LengthSquared(Speed) / MaxSpeed);
            Vector2 partWith = Vector2Geometry.AngleToVector(speedAngle, partWithLength * accelerationLength);
            Vector2 part90 = Vector2Geometry.AngleToVector(speedAngle + .5f * (float)Math.PI,
                -(float)Math.Sin(angle) * accelerationLength);
            Acceleration = partWith + part90;
            Speed += Acceleration - Speed * .1f * Engine.DeltaTime;
            Speed = Vectors.Rotate(Speed, FlyError);
            // FNA Vector2.Lerp evaluates start + (end - start) * amount per component.
            var amount = 3f * Engine.DeltaTime;
            NewSpeed = new Vector2(NewSpeed.X + (OutdatedSpeed.X - NewSpeed.X) * amount,
                NewSpeed.Y + (OutdatedSpeed.Y - NewSpeed.Y) * amount);
            Position += NewSpeed * Engine.DeltaTime;
        }

        public VertexLightState GetNearestLight(Vector2 nearestTo)
        {
            var lights = ((ISourceTrackedComponents)((ISourceComponentIndexScene)Scene).ComponentIndex)
                .GetTrackedComponents("Celeste.VertexLight");
            VertexLightState nearest = null;
            if (lights == null) return null;
            float nearestDistance = 0f;
            foreach (VertexLightState light in lights)
            {
                float distance = Vectors.DistanceSquared(nearestTo,
                    ((ISourceEntityPosition)light.Entity).Position + light.Position);
                if ((nearest == null || distance < nearestDistance) && light.Entity is not SourceActorEntityState)
                {
                    nearest = light;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private IEnumerator FlyUpdate()
        {
            while (true)
            {
                FlyError = (SourceRandomMath.NextFloat(Random) - .5f) * .5f * (float)Math.PI;
                OutdatedSpeed = Speed;
                yield return SourceRandomMath.NextFloat(Random) * .05f + .2f;
            }
        }
    }
}
