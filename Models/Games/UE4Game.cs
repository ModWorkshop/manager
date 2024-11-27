using MWSManager.Structures;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MWSManager.Models.Games;

// This class is primarily meant to deal with UE4 games (probably UE5 too). UE3 and below are not supported.
public class UE4Game : Game
{
    public string UnrealName = "";
    public UE4Game(string name, string path, dynamic? extraData = null) : base(name, path)
    {
        ExtraData = extraData; // A problem with rider for some reason, can't give it to parent ctor
        UnrealName = extraData?.UnrealName ?? name;

        ModFileDirs.Add($"{UnrealName}/Content/Paks/~mods/*.pak");
        ModFileDirs.Add($"{UnrealName}/Content/Paks/LogicMods/*.pak");
        ModDirs.Add($"{UnrealName}/Binaries/Win64/Mods");
        ModDirs.Add($"{UnrealName}/Content/Paks/LogicMods");
    }

    protected override void ProcessMod(Mod mod)
    {
        var installDir = mod.InstallDir;

        if (installDir == null)
            return;

        installDir = installDir.Replace($"{Name}/", "");
        if (installDir.Contains("*"))
            installDir = Path.GetDirectoryName(installDir);

        switch (installDir)
        {
            case "Content/Paks/~mods":
                ProcessPakMod(mod);
                break;
            case "Content/Paks/LogicMods":
                ProcessPakMod(mod);
                break;
            case "Binaries/Win64/Mods":
                ProcessPakMod(mod);
                break;
            default:
                Trace.TraceError("Invalid mod directory");
                break;
        };
    }

    protected void ProcessPakMod(Mod mod)
    {

    }

    public override bool CheckPossibleModInNode(PathNode node, List<Mod> Mods)
    {
        var name = node.Name;
        string? installDir = null;

        if (node.IsFile)
        {
            var ext = Path.GetExtension(name);

            // BLT mods are automatically to be installed in mods folder!
            if (name == "main.lua" || name == "main.dll")
            {
                installDir = $"{UnrealName}/Binaries/Win64/Mods";
            }
            else if (ext == ".utoc")
            {
                // While this is not the smartest way, I believe it should more or less work.
                // I tried using CUE4Parse, but the docs are quite lacking,
                // It's possible also that some ucas/utoc isn't supported by it.
                if (File.ReadAllText(node.FullPath).Contains("ModActor.uasset"))
                {
                    installDir = $"{UnrealName}/Content/Paks/LogicMods";
                }
            }
            else if (ext == ".pak")
            {
                installDir = $"{UnrealName}/Content/Paks/~mods";
            }
        }

        if (installDir != null)
        {
            var mod = new Mod(this, node.Parent!.FullPath);
            Mods.Add(mod);
            mod.InstallDir = installDir;

            return true;
        }

        return false;
    }
}
