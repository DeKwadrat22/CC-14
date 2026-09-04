using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Spawn)]
    public sealed partial class DeleteComponent : LocalizedEntityCommands
    {
        [Dependency] private IComponentFactory _compFactory = default!;
        // Claw Command - admin logging for deletecomponent
        [Dependency] private IAdminLogManager _adminLogger = default!;

        public override string Command => "deletecomponent";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    shell.WriteLine(Loc.GetString($"shell-need-exactly-one-argument"));
                    break;
                default:
                    var name = string.Join(" ", args);

                    if (!_compFactory.TryGetRegistration(name, out var registration))
                    {
                        shell.WriteLine(Loc.GetString($"cmd-deletecomponent-no-component-exists", ("name", name)));
                        break;
                    }

                    var componentType = registration.Type;
                    var components = EntityManager.GetAllComponents(componentType, true);

                    var i = 0;

                    foreach (var (uid, component) in components)
                    {
                        EntityManager.RemoveComponent(uid, component);
                        i++;
                    }

                    // Claw Command - admin logging
                    var adminName = shell.Player?.Name ?? "Server";
                    _adminLogger.Add(LogType.AdminCommands, LogImpact.Extreme, $"{adminName} deleted {i} components of type {name}");

                    shell.WriteLine(Loc.GetString($"cmd-deletecomponent-success", ("count", i), ("name", name)));

                    break;
            }
        }
    }
}
