using System.Collections;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Geometry;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Game.Entities.Npcs;
using Celeste.Game.Components.Talking;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.HonlyHelper.Entities.Npcs
{
    [CustomEntity("HonlyHelper/PettableCat")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.HonlyHelper.PettableCat")]
    public class PettableCat : OriginalNpcState
    {
        private readonly Sprite CatSprite;
        private readonly Sprite ThePetterSprite;
        private readonly string catFlag;
        private SimulationCoroutine pettingRoutine;
        private Vector2 CatAnchor;

        public PettableCat(Vector2 position, string CatFlag, ISourceEngineContext engine)
            : base(engine, position)
        {
            CatSprite = new Sprite(engine, ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas,
                "characters/HonlyHelper/pettableCat/");
            CatSprite.AddLoop("idle", "spoons_idle", 0.15f);
            CatSprite.Add("pet", "spoons_pet", 0.15f);
            CatSprite.Play("idle");
            OwnedComponents.Add(CatSprite);

            ThePetterSprite =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory.Create("HonlyHelper_CatPetter");

            catFlag = CatFlag ?? "CatHasBeenPet";
        }

        public PettableCat(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : this(data.Position + offset, data.Attr("catFlag"), engine)
        {
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            OwnedComponents.Add(Talker = new OriginalTalkComponent((IOriginalTalkProcess)Engine,
                new IntRect(-30, 0, 64, 8), new Vector2(2f, -4f), OnPetting));
            CatAnchor = CatSprite.Position;
        }

        private void OnPetting(RoomPlayerSession player)
        {
            Level.StartCutscene(OnPettingEnd);
            OwnedComponents.Add(pettingRoutine = new SimulationCoroutine(Engine, ThePetting(player)));
        }

        private IEnumerator ThePetting(RoomPlayerSession player)
        {
            yield return PlayerApproachLeftSide(player, turnToFace: true, 6f);
            CatSprite.Play("pet");
            CatSprite.Position = CatAnchor + new Vector2(-4f, -8f);

            player.Sprite.Visible = player.Hair.Visible = false;
            ThePetterSprite.Play("pet", restart: true);
            OwnedComponents.Add(ThePetterSprite);
            ThePetterSprite.Position = CatAnchor + new Vector2(-22f, -24f);

            Process.AudioSession.Play("event:/HonlyHelper/catsfx", Center, Process.SaveContext);
            yield return 2f;
            Level.EndCutscene();
            OnPettingEnd(Level);
            CatSprite.Play("idle");
            CatSprite.Position = CatAnchor;
            ((OriginalLevelSceneRoot)Scene).Session.SetFlag(catFlag);
        }

        private void OnPettingEnd(OriginalLevelSceneRoot level)
        {
            if (((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>() is RoomPlayerSession
                player)
            {
                player.Sprite.Visible = player.Hair.Visible = true;
                player.Machine.Locked = false; // silently set by PlayerApproachLeftSide :(
                player.Machine.State = 0;
            }

            pettingRoutine.Cancel();
            pettingRoutine.RemoveSelf();
            CatSprite.Play("idle");
            CatSprite.Position = CatAnchor;
            ThePetterSprite.RemoveSelf();
        }
    }
}
