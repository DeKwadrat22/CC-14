using Robust.Shared.Console;
using Robust.Shared.Player;
using Content.Shared.Administration;
using Content.Server.Spawners.Components;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared.Preferences;
using Content.Server.Preferences.Managers;
using Content.Shared.Players;
using Content.Server.Administration.Managers;
namespace Content.Server._ClawCommand.Centcomm;

internal sealed partial class CentcommSystem : EntitySystem
{
    [Dependency] private IConsoleHost _consoleHost = default!;
    [Dependency] private MindSystem _mindSystem = default!;
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;

    public override void Initialize()
    {
        base.Initialize();

        _consoleHost.RegisterCommand("centcomm", Loc.GetString("centcomm-spawn-command-desc"), "centcomm",
            CentcommCallback);

        _consoleHost.RegisterCommand("centcomm.officer", Loc.GetString("centcomm-spawn-command-desc"), "centcomm.officer",
            CentcommOfficerCallback);
    }

    [AnyCommand]
    public void CentcommCallback(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is null)
        {
            shell.WriteError("A player must execute this command.");
            return;
        }

        if (shell.Player is not ICommonSession player)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        var data = player.ContentData();
        if (data?.UserId == null)
        {
            shell.WriteError(Loc.GetString("shell-entity-is-not-mob"));
            return;
        }

        if (!_adminManager.HasAdminFlag(shell.Player, AdminFlags.VIPPlus)
            && !_adminManager.HasAdminFlag(shell.Player, AdminFlags.Admin))
        {
            shell.WriteError("You need to be a VIP Plus tier patron for access to this command.");
            return;
        }

        if (shell.Player.AttachedEntity is null)
        {
            shell.WriteError("You must be attached to an entity, observe as ghost.");
            return;
        }

        if (!_mindSystem.TryGetMind(shell.Player.AttachedEntity.Value, out var mindId, out _))
        {
            shell.WriteError("You must have a mind, try observe as ghost.");
            return;
        }

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (points.MoveNext(out _, out var spawnPoint, out var xform))
        {
            if (spawnPoint.Job?.Id != "CentralCommandOfficial")
                continue;

            var character = (HumanoidCharacterProfile)_prefs.GetPreferences(data.UserId).SelectedCharacter;

            var mob = _entityManager.System<StationSpawningSystem>()
                .SpawnPlayerMob(xform.Coordinates, "CentralCommandOfficial", character, null);

            _mindSystem.TransferTo(mindId, mob);

            shell.WriteLine("Success.");
            return;
        }

        shell.WriteError("No CentralCommandOfficial spawn point found on the map.");
    }

    [AnyCommand]
    public void CentcommOfficerCallback(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is null)
        {
            shell.WriteError("A player must execute this command.");
            return;
        }

        if (shell.Player is not ICommonSession player)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        var data = player.ContentData();
        if (data?.UserId == null)
        {
            shell.WriteError(Loc.GetString("shell-entity-is-not-mob"));
            return;
        }

        if (!_adminManager.HasAdminFlag(shell.Player, AdminFlags.VIPPlus)
            && !_adminManager.HasAdminFlag(shell.Player, AdminFlags.Admin))
        {
            shell.WriteError("You need to be a VIP Plus tier patron for access to this command.");
            return;
        }

        if (shell.Player.AttachedEntity is null)
        {
            shell.WriteError("You must be attached to an entity, observe as ghost.");
            return;
        }

        if (!_mindSystem.TryGetMind(shell.Player.AttachedEntity.Value, out var mindId, out _))
        {
            shell.WriteError("You must have a mind, try observe as ghost.");
            return;
        }

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (points.MoveNext(out _, out var spawnPoint, out var xform))
        {
            if (spawnPoint.Job?.Id != "CentralCommandOfficial")
                continue;

            var character = (HumanoidCharacterProfile)_prefs.GetPreferences(data.UserId).SelectedCharacter;

            var mob = _entityManager.System<StationSpawningSystem>()
                .SpawnPlayerMob(xform.Coordinates, "CentralCommandOfficer", character, null);

            _mindSystem.TransferTo(mindId, mob);

            shell.WriteLine("Success.");
            return;
        }

        shell.WriteError("No CentralCommandOfficial spawn point found on the map.");
    }
}
