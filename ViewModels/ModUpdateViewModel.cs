﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MWSManager.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MWSManager.ViewModels;

public partial class ModUpdateViewModel : ViewModelBase
{
    [Reactive]
    private ModUpdate update;

    [ObservableAsProperty]
    public double downloadPercent;

    [Reactive]
    private bool canUpdate = true;

    private IObservable<bool> canExecuteDownloadUpdate;

    public ModUpdateViewModel(ModUpdate update)
    {
        Update = update;

        downloadPercentHelper = this.WhenAnyValue(x => x.Update.DownloadProgress)
                .Select(x => x * 100)
                .ToProperty(this, x => x.DownloadPercent);

        canExecuteDownloadUpdate = this.WhenAnyValue(x => x.CanUpdate);

        if (update.FreshInstall)
        {
            CanUpdate = false;
        }
    }

    [ReactiveCommand(CanExecute = nameof(canExecuteDownloadUpdate))]
    private void DownloadUpdate()
    {
        CanUpdate = false;
        Update.DownloadAndInstallUpdate();
    }
}
