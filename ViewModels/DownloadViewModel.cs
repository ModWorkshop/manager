using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;

namespace MWSManager.ViewModels;

public partial class DownloadViewModel : ViewModelBase
{
	[Reactive]
	private string name = "Unknown download";

    [Reactive]
    private string modName = "Unknown mod";

    [Reactive]
    private int progress;

    [Reactive]
    private bool showProgressPercent = true;

    [Reactive]
    private string progressText = "Downloading";
}