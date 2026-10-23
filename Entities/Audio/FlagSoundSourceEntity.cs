using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Audio;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Audio.Sources;

namespace Celeste.Mods.HonlyHelper.Entities.Audio
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.HonlyHelper.FlagSoundSourceEntity")]
    [CustomEntity("HonlyHelper/FlagSoundSource")]
    public sealed class FlagSoundSourceEntity : SceneEntityState
    {
        private readonly string eventName;
        private readonly string flagName;
        private readonly bool fademode;
        private readonly SoundSourceComponentState sfx;
        private bool playing;

        public FlagSoundSourceEntity(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            fademode = data.Bool("AllowFade", false);
            flagName = data.Attr("flag", "");
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate.Value;
            Add(sfx = new SoundSourceComponentState(engine));
            eventName = ((IOriginalSfxEnvironment)engine).Sfx.EventnameByHandle(data.Attr("sound", ""));
            Visible = true;
            Depth = -8500;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (((OriginalLevelSceneRoot)Scene).Session.GetFlag(flagName))
            {
                sfx.Play(eventName);
                playing = true;
            }
        }

        public override void Update()
        {
            base.Update();
            if (!((OriginalLevelSceneRoot)Scene).Session.GetFlag(flagName) && playing)
            {
                sfx.Stop(fademode);
                playing = false;
            }
            else if (((OriginalLevelSceneRoot)Scene).Session.GetFlag(flagName) && !playing)
            {
                sfx.Play(eventName);
                playing = true;
            }
        }
    }
}
