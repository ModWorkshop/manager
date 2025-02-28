using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MWSManager.Services;
using MWSManager.ViewModels;
using Newtonsoft.Json;
using Serilog;
using SharpCompress;
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
    }

    public async override Task CheckMultipleUpdates(List<ModUpdate> updates)
    {
        for (int i = 0; i < updates.Count; i+=100)
        {
            var client = Utils.GetHTTPClient();
            var builder = new UriBuilder("https://api.modworkshop.net/mods/versions");
            var query = HttpUtility.ParseQueryString(builder.Query);
            builder.Port = -1;

            for (int j = i; j < updates.Count; j++)
            {
                query.Add("mod_ids[]", updates[j].Id);
            }

            builder.Query = query.ToString();
            var versions = await client.GetStringAsync(builder.ToString());
            try
            { 
                Log.Information(versions);
                var modVersions = JsonConvert.DeserializeObject<Dictionary<string, string>>(versions);
                foreach (var update in updates)
                {
                    if (modVersions[update.Id] != update.Version)
                    {
                        update.RaiseHasUpdate(modVersions[update.Id]);
                    }
                }
            }
            catch (Exception e) {
                Log.Error(e.Message);
            }
        }
    }

    
}

public class ModWorkshopFile : Provider
{
    public ModWorkshopFile()
    {
        Name = "mws-file";
        DownloadURL = "https://api.modworkshop.net/files/$Id$/download";
    }

    public async override Task CheckMultipleUpdates(List<ModUpdate> updates)
    {
        for (int i = 0; i < updates.Count; i += 100)
        {
            var client = Utils.GetHTTPClient();
            var builder = new UriBuilder("https://api.modworkshop.net/files/versions");
            var query = HttpUtility.ParseQueryString(builder.Query);
            builder.Port = -1;

            for (int j = i; j < updates.Count; j++)
            {
                query.Add("file_ids[]", updates[j].Id);
            }

            builder.Query = query.ToString();
            try
            {
                var versions = await client.GetStringAsync(builder.ToString());
                Log.Information(versions);
                var fileVersions = JsonConvert.DeserializeObject<Dictionary<string, string>>(versions);
                foreach (var update in updates)
                {
                    if (fileVersions[update.Id] != update.Version)
                    {
                        update.RaiseHasUpdate(fileVersions[update.Id]);
                    }
                }
            }
            catch (Exception) { }
        }
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

        foreach (var game in GamesService.Instance.Games)
        {
            if (game.MWSId == mwsMod.game_id)
            {
                Log.Information("New mods {0}", mwsMod.name);
                var update = new ModUpdate(game, $"{mwsMod.name} ({fileData.name})", Name, id, mwsMod.version);
                UpdatesService.Instance.AddUpdate(update);
                DownloadAndInstall(update);
            }
        }
    }
}