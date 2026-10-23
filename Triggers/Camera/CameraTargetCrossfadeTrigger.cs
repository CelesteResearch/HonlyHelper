// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Framework;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Entities.Triggers.Camera;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.HonlyHelper.Triggers.Camera
{
    [SceneTracked]
    [CustomEntity("HonlyHelper/CameraTargetCrossfadeTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.HonlyHelper.CameraTargetCrossfadeTrigger")]
    public class CameraTargetCrossfadeTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly float lerpAToB;
        private readonly Vector2 aToB;
        public Vector2 TargetA;
        public Vector2 TargetB;
        public float LerpStrengthA;
        public float LerpStrengthB;
        public PositionModes PositionMode;
        public bool XOnly;
        public bool YOnly;
        public string DeleteFlag;

        public CameraTargetCrossfadeTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                SourceVector2Math.Add(data.Position, offset), data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            var halfScreen = SourceVector2Math.Multiply(new Vector2(320f, 180f), .5f);
            TargetA = SourceVector2Math.Subtract(SourceVector2Math.Add(data.Nodes[0], offset), halfScreen);
            TargetB = SourceVector2Math.Subtract(SourceVector2Math.Add(data.Nodes[1], offset), halfScreen);
            LerpStrengthA = data.Float("lerpStrengthA");
            LerpStrengthB = data.Float("lerpStrengthB");
            PositionMode = data.Enum("positionMode", PositionModes.NoEffect);
            XOnly = data.Bool("xOnly");
            YOnly = data.Bool("yOnly");
            DeleteFlag = data.Attr("deleteFlag");
            aToB = SourceVector2Math.Subtract(TargetB, TargetA);
            lerpAToB = LerpStrengthB - LerpStrengthA;
        }

        public override void OnStay(RoomPlayerSession player)
        {
            if (!string.IsNullOrEmpty(DeleteFlag) &&
                ((OriginalLevelSceneRoot)Scene).Session.GetFlag(DeleteFlag)) return;
            float lerpPos = SourceFrameworkMath.Clamp(GetPositionLerp(player, PositionMode), 0f, 1f);
            player.CameraAnchor = SourceVector2Math.Add(TargetA, SourceVector2Math.Multiply(aToB, lerpPos));
            player.CameraAnchorLerp = SourceVector2Math.Multiply(Vector2.One, LerpStrengthA + lerpAToB * lerpPos);
            player.CameraAnchorIgnoreX = YOnly;
            player.CameraAnchorIgnoreY = XOnly;
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            foreach (var trigger in player.PlayerTriggers.Inside)
                if (IsCameraAnchor(trigger))
                    return;
            player.CameraAnchorIgnoreX = false;
            player.CameraAnchorIgnoreY = false;
            player.CameraAnchorLerp = Vector2.Zero;
        }

        private bool IsCameraAnchor(object trigger)
        {
            if (trigger is OriginalCameraTargetTriggerState or OriginalCameraAdvanceTargetTriggerState
                or CameraTargetCrossfadeTrigger) return true;
            var types = ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery;
            for (var type = trigger.GetType(); type != null; type = type.BaseType)
                if (types.RuntimeTypeNameMatches(type, "Celeste.Mod.HonlyHelper.CameraTargetCornerTrigger"))
                    return true;
            return false;
        }
    }
}
