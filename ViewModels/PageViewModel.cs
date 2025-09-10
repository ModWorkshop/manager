using System;
using System.Collections.Generic;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;

namespace MWSManager.ViewModels;

public partial class PageViewModel : ViewModelBase
{
    [Reactive]
    protected string name = "";

    [Reactive]
    protected string thumbnail = "";

    public virtual void OnPageOpened()
    {
    }

    [ReactiveCommand]
    private void SelectRoute()
    {
        MainWindowViewModel.Instance.CurrentPage = this;
        MainWindowViewModel.Instance.CurrentPageName = Name;
    }
}