using System;
using System.Collections.Generic;
using DynamicData.Binding;
using MWSManager.Models;
using MWSManager.Services;
using ReactiveUI;

namespace MWSManager.ViewModels;

public partial class SettingsPageViewModel : PageViewModel
{
    public Settings Data => SettingsService.Data;

    public string AppVersion => GetType().Assembly.GetName().Version.ToString();

    public SettingsPageViewModel()
    {
        Thumbnail = "avares://MWSManager/Assets/Settings.png";

        Data.WhenAnyPropertyChanged().Subscribe(_ => SettingsService.Instance.Save());
    }
}