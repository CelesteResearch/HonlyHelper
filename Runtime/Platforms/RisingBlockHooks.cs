using System;
using System.Collections;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Platforms.FallingBlocks;
using Celeste.Mods.HonlyHelper.Entities.Platforms.Moving;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.HonlyHelper.Runtime.Platforms
{
    public sealed class RisingBlockHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook sequence;

        public RisingBlockHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            sequence = new Hook(typeof(OriginalFallingBlockState).GetMethod("Sequence",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                (Func<Func<OriginalFallingBlockState, IEnumerator>, OriginalFallingBlockState, IEnumerator>)Run);
        }

        private IEnumerator Run(Func<OriginalFallingBlockState, IEnumerator> orig, OriginalFallingBlockState self)
        {
            return self is RisingBlock block && ReferenceEquals(block.EngineContext, engine)
                ? block.Rise()
                : ForwardOriginal(orig, self);
        }

        private static IEnumerator ForwardOriginal(Func<OriginalFallingBlockState, IEnumerator> orig,
            OriginalFallingBlockState self)
        {
            var enumerator = orig(self);
            while (enumerator.MoveNext()) yield return enumerator.Current;
        }

        public void Dispose() => sequence.Dispose();
    }
}
