using Avalonia.Collections;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using GameFinder.Common;
using MWSManager.Models.Games;
using MWSManager.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MWSManager.ViewModels;


public partial class GamePageViewModel : PageViewModel
{
    public static string[] validArchiveExt = { "zip", "7z", "rar", "xz", "gz", "tar" };

    public Game Game { get; }

    public SourceList<ModViewModel> Mods { get; set; } = new();

    [Reactive]
    private ModViewModel? selectedMod;

    [Reactive]
    private ModInfoViewModel modInfo;

    [ObservableAsProperty]
    public bool hasMods = false;

    [Reactive]
    private ReadOnlyObservableCollection<ModViewModel> orderedMods;

    [Reactive]
    private string searchModQuery = "";

    public GamePageViewModel(Game game) {
        Game = game;
        Thumbnail = game.Thumbnail;
        ModInfo = new ModInfoViewModel();

        this.WhenAnyValue(x => x.SelectedMod).Subscribe(x => OnSelectedModChanged());

        hasModsHelper = Mods.CountChanged.Select(x => x > 0).ToProperty(this, x => x.HasMods);

        Game.Mods.ToObservableChangeSet().Subscribe(_ => LoadMods());

        Mods.Connect()
            .AutoRefresh(x => x.HasUpdates)
            .AutoRefreshOnObservable(x => this.WhenAnyValue(x => x.SearchModQuery))
            .Sort(SortExpressionComparer<ModViewModel>.Descending(x => x.HasUpdates ? 1 : 0))
            .Filter(x => SearchModQuery.Length == 0 || Regex.IsMatch(x.Mod.Name.ToLower(), SearchModQuery.ToLower()))
            .Bind(out orderedMods)
            .Subscribe();
    }

    public void RefreshMods()
    {
        Game.LookForMods(true);
    }

    public override void OnPageOpened()
    {
        Log.Information("Ok");
        LoadMods();
        Log.Information("{0}", Mods.Count);
    }

    public void LoadMods()
    {
        Log.Information("Load Mods");
        Mods.Clear();

        var newMods = new List<ModViewModel>();
        foreach (var mod in Game.Mods)
        {
            newMods.Add(new ModViewModel(mod));
        }

        newMods = newMods.OrderByDescending(x => x.HasUpdates ? 1 : 0).ToList();

        Mods.AddRange(newMods);
    }

    void OnSelectedModChanged()
    {
        ModInfo.Mod = selectedMod?.Mod ?? null;
    }
}
