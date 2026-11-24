using System.IO;
using System.Linq;
using Content.Shared._Blimpuf.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client._Blimpuf.Administration.Commands;

/// <summary>
/// Loads a character profile from the file picker or client user data.
/// </summary>
public sealed class LoadCharacterCommand : IConsoleCommand
{
    public string Command => "loadcharacter";
    public string Description => "Spawn a character from a profile YAML file at your current location.";
    public string Help => "loadcharacter [path to a .yml file in client user data]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Help);
            return;
        }

        try
        {
            var path = args.Length == 1 ? new ResPath(args[0]).ToRootedPath() : (ResPath?) null;
            if (path is { } profilePath && !profilePath.Extension.Equals("yml", StringComparison.OrdinalIgnoreCase))
            {
                shell.WriteError("Select a .yml character profile.");
                return;
            }

            await using var file = path is { } userPath
                ? IoCManager.Resolve<IResourceManager>().UserData.OpenRead(userPath)
                : await IoCManager.Resolve<IFileDialogManager>().OpenFile(
                    new FileDialogFilters(new FileDialogFilters.Group("yml")), FileAccess.Read);
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

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var userData = IoCManager.Resolve<IResourceManager>().UserData;
        var options = CompletionHelper.UserFilePath(args[0], userData)
            .Where(option => option.Flags.HasFlag(CompletionOptionFlags.PartialCompletion)
                || option.Value.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));
        return CompletionResult.FromHintOptions(options, "Character profile (.yml)");
    }
}
