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
    private double downloadPercent;

    private IObservable<bool> canExecuteDownloadUpdate;

    public ModUpdateViewModel(ModUpdate update)
    {
        Update = update;

        downloadPercentHelper = this.WhenAnyValue(x => x.Update.DownloadProgress)
                .Select(x => x * 100)
                .ToProperty(this, x => x.DownloadPercent);

        canExecuteDownloadUpdate = Update.WhenAnyValue(x => x.Status).Select(x => x == ModUpdateStatus.Waiting);
    }

    [ReactiveCommand(CanExecute = nameof(canExecuteDownloadUpdate))]
    private void DownloadUpdate()
    {
        Update.DownloadAndInstallUpdate();
    }
}
