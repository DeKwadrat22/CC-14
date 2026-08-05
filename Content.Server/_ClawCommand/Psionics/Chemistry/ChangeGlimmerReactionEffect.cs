using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.Chemistry.ReactionEffects;

/// <summary>
///     Applies <see cref="ChangeGlimmerReactionEffect"/>. The effect declaration itself lives in
///     Content.Shared (Psionics/Glimmer/ChangeGlimmerReactionEffect.cs) because reaction prototypes are
///     parsed client-side too; only the behaviour is server-side.
/// </summary>
public sealed partial class ChangeGlimmerEntityEffectSystem : EntityEffectSystem<MobStateComponent, ChangeGlimmerReactionEffect>
{
    [Dependency] private GlimmerSystem _glimmer = default!;

    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<ChangeGlimmerReactionEffect> args)
    {
        _glimmer.Glimmer += args.Effect.Count;
    }
}
