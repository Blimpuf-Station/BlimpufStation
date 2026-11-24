using System.IO;
using Content.Shared._Blimpuf.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;

namespace Content.Client._Blimpuf.Administration.Commands;

/// <summary>
/// Opens a picker for an exported character profile and sends the selected YAML to the server.
/// </summary>
public sealed class LoadCharacterCommand : IConsoleCommand
{
    public string Command => "loadcharacter";
    public string Description => "Spawn a character from a profile YAML file at your current location.";
    public string Help => Command;

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Help);
            return;
        }

        var dialogs = IoCManager.Resolve<IFileDialogManager>();
        try
        {
            await using var file = await dialogs.OpenFile(new FileDialogFilters(new FileDialogFilters.Group("yml")), FileAccess.Read);
            if (file == null)
                return;

            using var reader = new StreamReader(file, leaveOpen: true);
            var yaml = await reader.ReadToEndAsync();
            IoCManager.Resolve<IEntityManager>().EntityNetManager.SendSystemNetworkMessage(new LoadCharacterEvent(yaml));
        }
        catch (Exception ex)
        {
            shell.WriteError($"Could not load the character file: {ex.Message}");
        }
    }
}
