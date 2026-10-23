using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.HonlyHelper.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.HonlyHelper.FireFlyDance")]
    [CustomEntity("HonlyHelper/FireFlyDance")]
    public sealed class FireFlyDance : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly int[] blinkIndexes = { 0, 20, 40, 60, 80, 110, 140, 170 };
        private readonly List<FireFly> fireFlyList = new();
        private float sineTimer;
        private float vibeTimer;
        private bool vibin;
        private bool dancing;

        public FireFlyDance(ISourceEngineContext engine, Vector2 position)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            dancing = false;
            Collider = new HitboxColliderGeometry(500f, 260f, -250f, -130f);
            Add(new PlayerColliderComponentState(OnCollide));
        }

        public FireFlyDance(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset)
        {
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    Vector2 position = new Vector2(-240f + i * 24f, -120f + j * 24f) + Position;
                    var fireFly = new FireFly(engine, position, true, true);
                    fireFlyList.Add(fireFly);
                    ((ISourceScene)Scene).SourceScene.AddEntity(fireFly);
                }
            }
        }

        public override void Update()
        {
            UpdateTimer();
            if (dancing)
            {
                int index = 0;
                int ring = 0;
                float direction = 1f;
                foreach (var fireFly in fireFlyList)
                {
                    if (index > 79)
                    {
                        ring = 1;
                        direction = -1f;
                    }

                    fireFly.BlinksSelf = false;
                    fireFly.ControlsSelf = false;
                    float radius = 56f + 32f * ring;
                    var offset = new Vector2(
                        radius * (float)Math.Sin((float)Math.PI * 2f * (index - 80 * ring) / (ring == 0 ? 80 : 120) +
                                                 sineTimer * direction),
                        radius * (float)Math.Cos((float)Math.PI * 2f * (index - 80 * ring) / (ring == 0 ? 80 : 120) +
                                                 sineTimer * direction));
                    fireFly.FlightGoal = Position + offset;
                    fireFly.BlinkTimer = 10f;
                    index++;
                }
            }

            base.Update();
        }

        private void UpdateTimer()
        {
            sineTimer += engine.DeltaTime * .4f;
            if (sineTimer > (float)Math.PI * 2f) sineTimer -= (float)Math.PI * 2f;
            if (!vibin)
            {
                if (vibeTimer > 0f) vibeTimer -= engine.DeltaTime * 9f;
                else dancing = false;
            }
            else if (vibeTimer < 180f) vibeTimer += engine.DeltaTime;
            else if (!dancing)
            {
                dancing = true;
                foreach (var fireFly in fireFlyList) fireFly.BlinkCancel = true;
                Add(new SimulationCoroutine(engine, BlinkBlonkin()));
            }

            vibin = false;
        }

        private IEnumerator BlinkBlonkin()
        {
            while (dancing)
            {
                for (int i = 0; i < blinkIndexes.Length; i++)
                {
                    fireFlyList[blinkIndexes[i]]
                        .Add(new SimulationCoroutine(engine, fireFlyList[blinkIndexes[i]].Blink()));
                    fireFlyList[blinkIndexes[i]].BlinkTimer = 10f;
                    blinkIndexes[i] += blinkIndexes[i] <= 79 ? 1 : -1;
                    if (blinkIndexes[i] > 79 && i < blinkIndexes.Length / 2) blinkIndexes[i] -= 80;
                    else if (blinkIndexes[i] < 80 && i > blinkIndexes.Length / 2 - 1) blinkIndexes[i] += 120;
                }

                yield return .025f;
            }

            yield return null;
        }

        private void OnCollide(RoomPlayerSession player)
        {
            if (!vibin) vibin = true;
        }
    }
}
