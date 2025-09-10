using MWSManager.Models.Games;
using MWSManager.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWSManager.Models;

public enum ModUpdateStatus
{
    Idle,
    Waiting,
    Downloading,
}

public partial class ModUpdate : ReactiveObject
{
    public Mod? Mod { get; }

    public Game Game { get; }

    public string Provider { get; }
    public string Id { get; }

    public string Name { get; set; }

    [Reactive]
    private string version;

    [Reactive]
    private string? nextVersion;

    [Reactive]
    private double downloadProgress;

    [Reactive]
    private ModUpdateStatus status = ModUpdateStatus.Idle;

    public bool FreshInstall { get; set; } = true;

    /// <summary>
    /// Creates a mod update for a mod
    /// </summary>
    public ModUpdate(Mod mod, string provider, string id, string version) : this(mod.Game, mod.Name, provider, id, version)
    {
        Mod = mod;
        FreshInstall = false;
    }

    /// <summary>
    /// Creates a mod update for a fresh install
    /// </summary>
    public ModUpdate(Game game, string name, string provider, string id, string version)
    {
        Name = name;
        Game = game;
        Provider = provider;
        Id = id;
        Version = version;

        this.WhenAnyValue(x => x.DownloadProgress)
            .Select(x => x == 1)
            .Subscribe(doneDownloading =>
            {
                if (doneDownloading)
                {
                    Status = ModUpdateStatus.Idle;
                }
            });
    }

    public void RaiseHasUpdate(string version)
    {
        Log.Information("Received next version! {0}", version);
        NextVersion = version;
        Status = ModUpdateStatus.Waiting;
    }

    public void DownloadAndInstallUpdate()
    {
        Status = ModUpdateStatus.Downloading;
        Locator.Current.GetService<UpdatesService>()!.DownloadAndInstall(this);
    }
}

