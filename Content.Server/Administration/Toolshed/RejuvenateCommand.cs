using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Administration.Systems;
using Content.Shared.Database;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed partial class RejuvenateCommand : ToolshedCommand
{
    // Claw Command - admin logging for rejuvenate
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IChatManager _chatManager = default!;

    private RejuvenateSystem? _rejuvenate;

    [CommandImplementation]
    public IEnumerable<EntityUid> Rejuvenate(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input)
    {
        _rejuvenate ??= GetSys<RejuvenateSystem>();

        foreach (var i in input)
        {
            _rejuvenate.PerformRejuvenate(i);

            // Claw Command - admin logging
            var adminName = ctx.Session?.Name ?? "Unknown";
            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{adminName} rejuvenated entity {i}");
            _chatManager.SendAdminAnnouncement(Loc.GetString("admin-log-rejuvenate", ("admin", adminName), ("entity", i)));

            yield return i;
        }
    }

    [CommandImplementation]
    public void Rejuvenate(IInvocationContext ctx)
    {
        _rejuvenate ??= GetSys<RejuvenateSystem>();
        if (ExecutingEntity(ctx) is not { } ent)
        {
            if (ctx.Session is {} session)
                ctx.ReportError(new SessionHasNoEntityError(session));
            else
                ctx.ReportError(new NotForServerConsoleError());
        }
        else
        {
            _rejuvenate.PerformRejuvenate(ent);

            // Claw Command - admin logging
            var adminName = ctx.Session?.Name ?? "Unknown";
            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{adminName} rejuvenated entity {ent}");
            _chatManager.SendAdminAnnouncement(Loc.GetString("admin-log-rejuvenate", ("admin", adminName), ("entity", ent)));
        }
    }
}
