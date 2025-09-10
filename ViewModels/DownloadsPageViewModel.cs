using DynamicData;
using DynamicData.Binding;
using MWSManager.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MWSManager.ViewModels;

public partial class DownloadsPageViewModel : PageViewModel
{
    public ObservableCollection<ModUpdateViewModel> Updates { get; } = [];

    [Reactive]
    private bool checkingUpdates = false;

    [ObservableAsProperty]
    public bool hasUpdates = false;

    private bool FirstOpen = false;

    private readonly UpdatesService _updatesService;

    public DownloadsPageViewModel(UpdatesService updatesService)
    {
        Thumbnail = "avares://MWSManager/Assets/Download.png";
        Name = "Downloads";

        hasUpdatesHelper = Updates.ToObservableChangeSet().Select(x => x.Count > 0).ToProperty(this, x => x.HasUpdates);

        _updatesService = updatesService;

        updatesService.Updates.ToObservableChangeSet()
            .AutoRefresh(x => x.FreshInstall)
            .AutoRefresh(x => x.NextVersion)
            .Subscribe(RegUpdates =>
            {
                foreach (var update in updatesService.Updates)
                {
                    if (!update.FreshInstall && update.NextVersion == null)
                        continue;

                    bool exists = false;
                    foreach (var muvm in Updates)
                    {
                        if (muvm.Update == update)
                            exists = true;
                    }

                    if (!exists)
                        Updates.Add(new ModUpdateViewModel(update));
                }

                // Remove updates that no longer exist
                foreach (var muvm in Updates.ToList())
                {
                    if (muvm.Update.DownloadProgress == 1 || muvm.Update.FreshInstall)
                    {
                        continue; // Ignore downloaded or install ones
                    }

                    var found = muvm.Update.FreshInstall;
                    foreach (var update in updatesService.Updates)
                    {
                        if (update == muvm.Update)
                            found = true;
                    }

                    if (!found)
                        Updates.Remove(muvm);
                }
            });
    }

    [ReactiveCommand(CanExecute = nameof(CheckingUpdates))]
    private async Task CheckUpdates()
    {
        CheckingUpdates = true; 
        await _updatesService.CheckForUpdates();
        CheckingUpdates = false;
    }
}