using Content.Server.Administration;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared._ClawCommand.Administration;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server._ClawCommand.Administration;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class PlayTimeEditorCommand : LocalizedCommands
{
    [Dependency] private EuiManager _euis = default!;

    public override string Command => "timetransferpanel";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        _euis.OpenEui(new PlayTimeEditorEui(), player);
    }
}

public sealed partial class PlayTimeEditorEui : BaseEui
{
    [Dependency] private IAdminManager _adminMan = default!;
    [Dependency] private ILogManager _log = default!;
    [Dependency] private IPlayerLocator _playerLocator = default!;
    [Dependency] private IServerDbManager _databaseMan = default!;

    private readonly ISawmill _sawmill;

    public PlayTimeEditorEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _log.GetSawmill("admin.playtime_editor");
    }

    public override PlayTimeEditorEuiState GetNewState()
    {
        var hasPermission = _adminMan.HasAdminFlag(Player, AdminFlags.Admin);
        return new PlayTimeEditorEuiState(hasPermission);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not PlayTimeEditorSubmitMessage message)
            return;

        ProcessChanges(message.TargetName, message.Adjustments, message.Overwrite);
    }

    public async void ProcessChanges(string targetName, List<PlayTimeAdjustment> adjustments, bool overwrite)
    {
        if (!_adminMan.HasAdminFlag(Player, AdminFlags.Admin))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId} tried to modify role time without admin flag)");
            return;
        }

        var targetData = await _playerLocator.LookupIdByNameAsync(targetName);
        if (targetData == null)
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId} tried to modify role time for non-existing player {targetName})");
            SendMessage(new PlayTimeEditorStatusMessage(Loc.GetString("playtime-editor-target-not-found"), Color.Red));
            return;
        }

        if (overwrite)
            OverwriteTime(targetData.UserId, adjustments);
        else
            AddTime(targetData.UserId, adjustments);
    }

    public async void OverwriteTime(NetUserId userId, List<PlayTimeAdjustment> adjustments)
    {
        var updates = new List<PlayTimeUpdate>();

        foreach (var adj in adjustments)
        {
            var duration = TimeSpan.FromMinutes(PlayTimeCommandUtilities.CountMinutes(adj.DurationText));
            updates.Add(new PlayTimeUpdate(userId, adj.RoleTracker, duration));
        }

        await _databaseMan.UpdatePlayTimes(updates);
        _sawmill.Info($"{Player.Name} ({Player.UserId} overwrote {updates.Count} trackers for {userId})");
        SendMessage(new PlayTimeEditorStatusMessage(Loc.GetString("playtime-editor-status-overwrite-success"), Color.LightGreen));
    }

    public async void AddTime(NetUserId userId, List<PlayTimeAdjustment> adjustments)
    {
        var existingTimes = await _databaseMan.GetPlayTimes(userId);

        Dictionary<string, TimeSpan> existingDict = new();
        foreach (var entry in existingTimes)
        {
            existingDict.Add(entry.Tracker, entry.TimeSpent);
        }

        var updates = new List<PlayTimeUpdate>();

        foreach (var adj in adjustments)
        {
            var duration = TimeSpan.FromMinutes(PlayTimeCommandUtilities.CountMinutes(adj.DurationText));
            if (existingDict.TryGetValue(adj.RoleTracker, out var existing))
                duration += existing;

            updates.Add(new PlayTimeUpdate(userId, adj.RoleTracker, duration));
        }

        await _databaseMan.UpdatePlayTimes(updates);
        _sawmill.Info($"{Player.Name} ({Player.UserId} added {updates.Count} trackers for {userId})");
        SendMessage(new PlayTimeEditorStatusMessage(Loc.GetString("playtime-editor-status-add-success"), Color.LightGreen));
    }

    public override async void Opened()
    {
        base.Opened();
        _adminMan.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _adminMan.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        StateDirty();
    }
}
