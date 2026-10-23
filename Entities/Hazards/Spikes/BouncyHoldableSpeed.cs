using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Members;
using Celeste.Everest.Reflection;
using Celeste.Scene.Entities;

namespace Celeste.Mods.HonlyHelper.Entities.Hazards.Spikes
{
    internal readonly struct BouncyHoldableSpeed
    {
        private sealed class Accessor
        {
            public Func<SceneEntityState, Vector2> Read;
            public Action<SceneEntityState, Vector2> Write;
        }

        private sealed class Owner
        {
            public readonly Dictionary<Type, Accessor> Accessors = new();
        }

        private static readonly ConditionalWeakTable<ISourceEngineContext, Owner> Owners = new();
        private readonly SceneEntityState entity;
        private readonly Accessor accessor;

        private BouncyHoldableSpeed(SceneEntityState entity, Accessor accessor)
        {
            this.entity = entity;
            this.accessor = accessor;
        }

        public static BouncyHoldableSpeed For(ISourceEngineContext engine, SceneEntityState entity)
        {
            var owner = Owners.GetValue(engine, _ => new Owner());
            var actualType = entity.GetType();
            if (!owner.Accessors.TryGetValue(actualType, out var accessor))
            {
                accessor = Find(((ISourceEverestReflectionEnvironment)engine).Reflection.SourceMembers, actualType);
                owner.Accessors.Add(actualType, accessor);
            }

            return new BouncyHoldableSpeed(entity, accessor);
        }

        private static Accessor Find(SourceMemberCatalog members, Type type)
        {
            for (; type != null; type = type.BaseType)
            {
                if (type == typeof(Celeste.Game.Gameplay.Holdables.TheoCrystal.TheoCrystalState))
                    return new Accessor
                    {
                        Read = entity => ((Celeste.Game.Gameplay.Holdables.TheoCrystal.TheoCrystalState)entity).Speed,
                        Write = (entity, value) =>
                            ((Celeste.Game.Gameplay.Holdables.TheoCrystal.TheoCrystalState)entity).Speed = value
                    };
                if (type == typeof(Celeste.Game.Gameplay.Holdables.Gliders.OriginalGliderState))
                    return new Accessor
                    {
                        Read = entity => ((Celeste.Game.Gameplay.Holdables.Gliders.OriginalGliderState)entity).Speed,
                        Write = (entity, value) =>
                            ((Celeste.Game.Gameplay.Holdables.Gliders.OriginalGliderState)entity).Speed = value
                    };
                if (members.ContainsSourceType(type))
                {
                    foreach (var definition in members.GetOriginalMemberDefinitions(type, true))
                        if (definition.Name == "Speed" && !definition.IsStatic && definition.CanRead)
                        {
                            var binding = members.RequireBinding(type, definition);
                            return new Accessor
                            {
                                Read = entity => (Vector2)binding.Read(entity),
                                Write = (entity, value) => binding.Write(entity, value)
                            };
                        }
                }
                else
                {
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                               BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                    MemberInfo member = type.GetField("Speed", flags);
                    member ??= type.GetProperty("Speed", flags);
                    if (member == null) continue;
                    var entity = Expression.Parameter(typeof(SceneEntityState));
                    var value = Expression.Parameter(typeof(Vector2));
                    var access = Expression.MakeMemberAccess(Expression.Convert(entity, type), member);
                    if (access.Type != typeof(Vector2)) return null;
                    return new Accessor
                    {
                        Read = Expression.Lambda<Func<SceneEntityState, Vector2>>(access, entity).Compile(),
                        Write = Expression.Lambda<Action<SceneEntityState, Vector2>>(
                            Expression.Assign(access, value), entity, value).Compile()
                    };
                }
            }

            return null;
        }

        public bool TryGet(string name, out Vector2 value)
        {
            value = accessor == null ? default : accessor.Read(entity);
            return accessor != null;
        }

        public void Set(string name, Vector2 value) => accessor.Write(entity, value);
    }
}
