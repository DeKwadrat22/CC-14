using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class SubGamemodesSystem : GameRuleSystem<SubGamemodesComponent>
{
    [Dependency] private ISharedPlayerManager _player = default!;

    protected override void Added(EntityUid uid, SubGamemodesComponent comp, GameRuleComponent rule, GameRuleAddedEvent args)
    {
        var picked = EntitySpawnCollection.GetSpawns(comp.Rules, RobustRandom);
        var players = GetPlayerCount();

        foreach (var id in picked)
        {
            if (GameTicker.IsIgnored(id))
                continue;

            // The ticker only enforces minPlayers in RoundStartAttemptEvent, which runs *after* the
            // preset's rules have already been added and started. That is too late for rules that do
            // work when added or started (loading a map, making ghost role spawners), so an undersized
            // sub-event could still leave its shuttle and antag ghost roles in the round even though
            // the rule itself got ended again. Check it here so the rule is never added at all.
            if (!HasEnoughPlayers(id, players))
            {
                Log.Info($"Skipping subgamemode {id} of {ToPrettyString(uid):rule}: {players} players, needs more.");
                continue;
            }

            Log.Info($"Starting gamerule {id} as a subgamemode of {ToPrettyString(uid):rule}");
            GameTicker.AddGameRule(id);
        }
    }

    /// <summary>
    /// Players available to the rule about to be added. Sub gamemodes are normally rolled from the
    /// lobby shortly before roundstart, but admins can add them midround too.
    /// </summary>
    private int GetPlayerCount()
    {
        return GameTicker.RunLevel == GameRunLevel.PreRoundLobby
            ? GameTicker.ReadyPlayerCount()
            : _player.PlayerCount;
    }

    private bool HasEnoughPlayers(EntProtoId id, int players)
    {
        if (!ProtoMan.Resolve(id, out var proto))
            return false;

        if (!proto.TryComp<GameRuleComponent>(out var rule, Factory))
            return true;

        return players >= rule.MinPlayers;
    }
}
