using DynamicData;
using DynamicData.Binding;
using MWSManager.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using ShadUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace MWSManager.ViewModels;

public partial class ModViewModel : ViewModelBase
{
    [Reactive]
    private Mod mod;

    [ObservableAsProperty]
    private string? authors;

    [ObservableAsProperty]
    private string thumbnail;

    [Reactive]
    private string? pageUrl;

    [Reactive]
    private bool hasUpdates = false;

    [ObservableAsProperty]
    private bool hasMod = false;

    public ObservableCollection<ModUpdateViewModel> Updates { get; } = [];

    public ModViewModel(Mod? mod=null)
    {
        Mod = mod;
        hasModHelper = this.WhenAnyValue(x => x.Mod)
            .Select(x => x != null)
            .ToProperty(this, x => x.HasMod);

        authorsHelper = this.WhenAnyValue(x => x.Mod)
            .Select(x => x?.Authors.Count > 0 ? String.Join(", ", x.Authors).Replace(" ", "") : "N/A")
            .ToProperty(this, x => x.Authors);

        thumbnailHelper = this.WhenAnyValue(x => x.Mod)
            .Select(x => x?.Thumbnail ?? "../Assets/DefaultModThumb.png")
            .ToProperty(this, x => x.Thumbnail);

        // I hate this. This would be a one liner in Vue...
        //hasUpdatesHelper = Mod.Updates.ToObservableChangeSet()
        //    .AutoRefresh(x => x.NextVersion)
        //    .Filter(x => x.NextVersion != null)
        //    .CountChanged()
        //    .Select(x => x.Count > 0)
        //    .ToProperty(this, x => x.HasUpdates);

        this.WhenAnyValue(x => x.Mod).WhereNotNull().Subscribe(mod =>
        {
            Updates.Clear();
            foreach (var update in mod.Updates)
            {
                Updates.Add(new ModUpdateViewModel(update));
            }

            HasUpdates = Updates.Count > 0;

            int? id = mod.Id;
            if (id == null && Updates.Count == 1 && Updates[0].Update.Provider == "mws")
            {
                id = int.Parse(Updates[0].Update.Id);
            }

            PageUrl = id != null ? $"https://modworkshop.net/mod/{id}" : null;
        });
    }

    [ReactiveCommand]
    private void BrowseToModPath()
    {
        try
        {
            if (Mod.IsFile)
            {
                Process.Start("explorer.exe", Path.GetDirectoryName(Mod.ModPath));
            }
            else
            {
                Process.Start("explorer.exe", Path.GetFullPath(Mod.ModPath));
            }
        }
        catch (Exception e)
        {
            Log.Information("{0}", e);
        }
    }

    [ReactiveCommand]
    private void OpenPageUrl()
    {
        if (PageUrl != null)
        {
            try
            {
                Process.Start(new ProcessStartInfo(PageUrl)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception e)
            {
                Log.Error("Couldn't open page URL: {0}. {1}", PageUrl, e);
            }
        }
    }

    [ReactiveCommand]
    private void DeleteMod()
    {
        try
        {
            Locator.Current.GetService<DialogManager>().CreateDialog(
                "Are you absolutely sure?",
                "This action cannot be undone. Deleting mods is an irreversible action.")
            .WithPrimaryButton("Continue",
                () => Mod.Delete(), DialogButtonStyle.Destructive)
            .WithCancelButton("Cancel")
            .WithMaxWidth(512)
            .Show();
        }
        catch (Exception)
        {
            Log.Error("Failed opening dialog!");
        }
    }
}