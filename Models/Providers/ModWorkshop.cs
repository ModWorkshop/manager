using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MWSManager.Services;
using MWSManager.ViewModels;
using Newtonsoft.Json;
using Serilog;
using SharpCompress;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MWSManager.Models.Providers;

public class MWSFile
{
    public int id { get; init; }
    public string mod_id { get; init; }
    public string name { get; init; }
    public string file { get; init; }
    public string type { get; init; }
    public string download_url { get; init; }
}

public class MWSMod
{
    public int game_id { get; init; }

    public string name { get; init; }
    public string version { get; init; }
}

public class ModWorkshop : Provider
{
    public ModWorkshop()
    {
        Name = "mws";
        DownloadURL = "https://api.modworkshop.net/mods/$Id$/download";
        CheckURL = "https://api.modworkshop.net/mods/$Id$/files/latest/version";
    }

    public override async Task DownloadAndInstallNewMod(string id)
    {
        var client = Utils.GetHTTPClient();

        var fileResponse = await client.GetStringAsync($"https://api.modworkshop.net/files/{id}");
        if (fileResponse == null)
            return;

        var fileData = JsonConvert.DeserializeObject<MWSFile>(fileResponse);

        var modResponse = await client.GetStringAsync($"https://api.modworkshop.net/mods/{fileData.mod_id}");
        if (modResponse == null)
        {
            Log.Information("Could not fetch MWS mod! (ID: {0})", id);
            return;
        }

        var mwsMod = JsonConvert.DeserializeObject<MWSMod>(modResponse);

        if (mwsMod == null)
        {
            Log.Information("Could not convert fetched MWS mod (ID: {0}) JSON data into a readable object!", id);
            return;
        }

        var gameService = Locator.Current.GetService<GamesService>();
        foreach (var game in gameService.Games)
        {
            if (game.MWSId == mwsMod.game_id)
            {
                Log.Information("New mods {0}", mwsMod.name);
                var update = new ModUpdate(game, $"{mwsMod.name} ({fileData.name})", Name, id, mwsMod.version);
                await DownloadAndInstall(update, "https://api.modworkshop.net/files/$Id$/download");
                break;
            }
        }
    }
}