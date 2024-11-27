using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MWSManager.Services;
using MWSManager.ViewModels;
using Newtonsoft.Json.Linq;
using Serilog;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace MWSManager.Models.Providers;

public class Provider
{
    public string Name { get; protected set; }

    /// <summary>
    /// The URL to chekc updates from. This should return a version as a regular string.
    /// For example: https://api.modworkshop.net/mods/$id$/version
    /// If your API has a more optimized way to do it (Like MWS's /mods/versions) you should override CheckMultipleUpdates
    /// </summary>
    protected string CheckURL;

    /// <summary>
    /// The download URL that the provider will use. This should be a direct download link.
    /// For example: https://api.modworkshop.net/mods/$id$/download
    /// If your API does not support that, you will have to override DownloadAndInstall and write your own code to do that
    /// </summary>
    protected string DownloadURL;

    /// <summary>
    /// Checks updates for multiple mod update objects.
    /// It's a good idea to override this method if your provider has a way to check multiple mods at once
    /// So you don't end up spamming its API / hitting ratelimit
    /// </summary>
    public virtual async Task CheckMultipleUpdates(List<ModUpdate> updates)
    {
        var client = Utils.GetHTTPClient();

        foreach (var update in updates)
        {
            var version = await client.GetStringAsync(CheckURL.Replace("$Id$", update.Id));
            if (version != null && version != update.Version)
            {
                update.RaiseHasUpdate(version);
            }
        }
    }

    public virtual async Task StartModUpdate(ModUpdate update)
    {
        try
        {
            await DownloadAndInstall(update);
        }
        catch (Exception e)
        {
            Log.Error("Failed to update mod with ID {0}. Error: {1}", update.Id, e);
            throw;
        }
    }

    /// <summary>
    /// Download a mod update and try installing it
    /// </summary>
    public virtual async Task DownloadAndInstall(ModUpdate update)
    {
        MainWindowViewModel.Instance?.SetCurrentPage("Downloads");
        var client = Utils.GetHTTPClient();
        var mod = update.Mod;
        var game = update.Game;

        Log.Information("Downloading {0} ({1})", update.Id, DownloadURL.Replace("$Id$", update.Id));

        using var fileResponse = await client.GetAsync(DownloadURL.Replace("$Id$", update.Id), HttpCompletionOption.ResponseHeadersRead);
        fileResponse.EnsureSuccessStatusCode();

        IEnumerable<string>? cdString;
        if (fileResponse.Content.Headers.TryGetValues("Content-Disposition", out cdString)) {
            var cd = new ContentDisposition(cdString.First());

            if (cd.FileName == null)
            {
                Log.Information("No file name!");
                return; // Assume we have a file name, for now. Not sure if there are situations where there isn't
            }

            using var modStream = await fileResponse.Content.ReadAsStreamAsync();
            using var memStream = new MemoryStream();

            long totalBytes = fileResponse.Content.Headers.ContentLength ?? 0;
            if (totalBytes == 0)
            { // Nothing to download so return
                Log.Information("Total bytes = 0");
                return;
            }

            var buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead = 0;

            while ((bytesRead = await modStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await memStream.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                update.DownloadProgress = ((double)totalBytesRead / totalBytes);
            }

            update.DownloadProgress = 1;
            memStream.Position = 0;

            Log.Information("Done downloading, installing..");
            var modInstall = new ModInstall(update, cd.FileName, memStream);
            modInstall.Install();
            update.NextVersion = null;
        } else
        {
            Log.Information("Failed to download, download has no Content-Disposition");
            Log.Information(fileResponse.Content.Headers.ToString());
        }      
    }

    /// <summary>
    /// Handles incoming URI scheme request for the provider.
    /// Should allow you to install mods from the browser, for example a user clicks on a file in MWS.
    /// </summary>
    public virtual async Task URISchemeHandle(string action, string id)
    {
        Log.Information("Action: {0}, ID: {1}", action, id);
        try
        {
            switch (action)
            {
                case "install":
                    await DownloadAndInstallNewMod(id);
                    break;
                default:
                    throw new NotImplementedException("Action not implemented for URIScheme!");
            }
        }
        catch (Exception e)
        {
            if (action == "install")
            {
                Log.Error("Failed to download and install mod with ID {0}. Error: {1}", id, e);
            }
            throw;
        }
        
    }

    /// <summary>
    /// Downloads a new mod by making a dummy mod with update
    /// </summary>
    public virtual async Task DownloadAndInstallNewMod(string id) { 
        
    }
}
