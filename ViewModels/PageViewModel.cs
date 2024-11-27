using System;
using System.Collections.Generic;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace MWSManager.ViewModels;

public partial class PageViewModel : ViewModelBase
{
    [Reactive]
    protected string thumbnail = "";

    public virtual void OnPageOpened()
    {
    }
}