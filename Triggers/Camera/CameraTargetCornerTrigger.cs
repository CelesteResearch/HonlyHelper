// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Framework;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Entities.Triggers.Camera;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.HonlyHelper.Triggers.Camera
{
    [SceneTracked]
    [CustomEntity("HonlyHelper/CameraTargetCornerTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.HonlyHelper.CameraTargetCornerTrigger")]
    public class CameraTargetCornerTrigger : SourceTriggerEntityState
    {
        public Vector2 Target;
        public float LerpStrength;
        public CameraAnchorCorner PositionMode;
        public bool XOnly;
        public bool YOnly;
        public string DeleteFlag;
        private readonly int modX;
        private readonly int modY;

        public CameraTargetCornerTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                SourceVector2Math.Add(data.Position, offset), data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            Target = SourceVector2Math.Subtract(SourceVector2Math.Add(data.Nodes[0], offset),
                SourceVector2Math.Multiply(new Vector2(320f, 180f), .5f));
            LerpStrength = data.Float("lerpStrength");
            PositionMode = data.Enum("positionMode", CameraAnchorCorner.BottomLeft);
            XOnly = data.Bool("xOnly");
            YOnly = data.Bool("yOnly");
            DeleteFlag = data.Attr("deleteFlag");
            switch (PositionMode)
            {
                case CameraAnchorCorner.BottomRight:
                    modX = 1;
                    break;
                case CameraAnchorCorner.TopLeft:
                    modY = 1;
                    break;
                case CameraAnchorCorner.TopRight:
                    modX = modY = 1;
                    break;
            }
        }

        public override void OnStay(RoomPlayerSession player)
        {
            if (!string.IsNullOrEmpty(DeleteFlag) &&
                ((OriginalLevelSceneRoot)Scene).Session.GetFlag(DeleteFlag)) return;
            player.CameraAnchor = Target;
            float strength = LerpStrength * Math.Abs(modX - SourceFrameworkMath.Clamp(
                GetPositionLerp(player, PositionModes.LeftToRight), 0f, 1f)) * Math.Abs(modY -
                SourceFrameworkMath.Clamp(GetPositionLerp(player, PositionModes.BottomToTop), 0f, 1f));
            player.CameraAnchorLerp = SourceVector2Math.Multiply(Vector2.One, strength);
            player.CameraAnchorIgnoreX = YOnly;
            player.CameraAnchorIgnoreY = XOnly;
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            foreach (var trigger in player.PlayerTriggers.Inside)
                if (trigger is OriginalCameraTargetTriggerState or OriginalCameraAdvanceTargetTriggerState
                    or CameraTargetCornerTrigger or CameraTargetCrossfadeTrigger)
                    return;
            player.CameraAnchorIgnoreX = false;
            player.CameraAnchorIgnoreY = false;
            player.CameraAnchorLerp = Vector2.Zero;
        }
    }
}
