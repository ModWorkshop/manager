using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using MWSManager.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;

namespace MWSManager.Models.Games.Diesel;

public class DieselGame : Game
{
    // Added for RWW2 due to its "mod_overrides" being in mods folder
    public string ModOverridesDir { get; init; } = "assets/mod_overrides";

    public readonly string[] ModOverridesFolders = [
        "anims", "units", "core", "effects", "environments", "fonts", "gamedata","guis", "levels",
        "lib", "movies", "physic_effects", "settings", "shaders", "soundbanks", "strings", "units"
    ];

    public DieselGame(string name, string path, dynamic? extraData = null) : base(name, path)
    {
         ModDirs = ["mods", "assets/mod_overrides"];

        SpecialPaths.Add("MODS", "mods");
        SpecialPaths.Add("OVERRIDES", "assets/mod_overrides");
    }

    public override bool CheckPossibleModInNode(PathNode node, List<Mod> Mods)
    {
        if (!node.IsFile && ModOverridesFolders.Contains(node.Name))
        {
            Mods.Add(new Mod(this, node.Parent.FullPath, ModOverridesDir));
            return true;
        }

        if (node.IsFile && Path.GetExtension(node.Name) == ".pdmod")
        {
            Dispatcher.UIThread.Post(async () =>
            {
                await MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                {
                    ContentTitle = "Unsupported!",
                    ContentMessage = "PDMODs are not supported and will not be supported!\nUse Bundle Modder if you are sure you want to install them.",
                    Icon = Icon.Forbidden,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Topmost = true,
                    ButtonDefinitions = [
                        new() { Name = "Ok" },
                    ]
                }).ShowAsync();
            });
            return true;
        }

        return false;
    }
}