// SPDX-License-Identifier: MIT

using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.HonlyHelper.Runtime;

namespace Celeste.Mods.HonlyHelper.Modules
{
    public sealed class HonlyHelperModule(ISourceEngineContext engine) : SourceEverestModule(engine)
    {
        private FloatyBackgroundHooks floatyBackground;
        private Runtime.Platforms.RisingBlockHooks risingBlocks;

        public override void Load()
        {
            risingBlocks = new Runtime.Platforms.RisingBlockHooks(SourceEngine);
            floatyBackground = new FloatyBackgroundHooks(SourceEngine);
            floatyBackground.Load();
        }

        public override void Unload()
        {
            risingBlocks?.Dispose();
            risingBlocks = null;
            Entities.FireFly.ResetPopulation(SourceEngine);
            floatyBackground?.Dispose();
            floatyBackground = null;
        }
    }
}
