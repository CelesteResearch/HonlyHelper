using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.LevelLoader.Terrain;
using Celeste.Game.Gameplay.Platforms.FallingBlocks;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.HonlyHelper.Entities.Platforms.Moving
{
    [EverestTrackedAs(typeof(OriginalFallingBlockState))]
    [CustomEntity("HonlyHelper/RisingBlock")]
    [RuntimeTypeAlias("Celeste.Mod.HonlyHelper.RisingBlock")]
    public partial class RisingBlock : OriginalFallingBlockState
    {
        private readonly string solidKey;
        private readonly string tilesKey;
        private readonly string platformKey;
        public ISourceEngineContext EngineContext => engine;

        public RisingBlock(Vector2 position, char tile, int width, int height, bool finalBoss, bool behind,
            bool climbFall, ISourceSolidWorld world, ISourceEngineContext engine)
            : base(position, tile, width, height, finalBoss, behind, climbFall, world, engine)
        {
            var catalog = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog;
            solidKey = catalog.Find(typeof(SourceSolidEntityState)).Name;
            tilesKey = catalog.Find(typeof(OriginalSolidTiles)).Name;
            platformKey = catalog.Find(typeof(SourcePlatformEntityState)).Name;
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            var position = data.Position + offset;
            return new RisingBlock(position, data.Char("tiletype", '3'), data.Width, data.Height, false,
                data.Bool("behind"), data.Bool("climbFall", true),
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(position, false), level.EngineContext);
        }
    }
}
