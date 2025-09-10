using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Steam;
using MWSManager.Models;
using NexusMods.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform;
using GameFinder.Common;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using GameFinder.StoreHandlers.EGS;
using MWSManager.Services;
using System.Threading.Tasks;
using Serilog;
using ReactiveUI;
using MWSManager.Models.Providers;
using ReactiveUI.SourceGenerators;
using ShadUI;
using Splat;

namespace MWSManager.ViewModels;

internal class GameDefinition
{
    public string? Name;
    public uint? SteamId;
    public uint? MWSId;
    public string? EpicId;
    public string? ClassName;
    public string? Icon;
    public string[]? ModDirs;

    public Dictionary<string, string>? SpecialPaths;

    public dynamic? ExtraData;
}

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<GamePageViewModel> Games { get; set; } = [];
    public ObservableCollection<PageViewModel> Pages { get; set; } = [];
    public ObservableCollection<PageViewModel> OtherPages { get; set; } = [];
    
    public DownloadsPageViewModel Downloads { get; }

    public static MainWindowViewModel? Instance { get; private set; }

    [Reactive]
    private PageViewModel currentPage;

    [Reactive]
    private string currentPageName;

    [Reactive]
    private GamePageViewModel? currentGame;

    [Reactive]
    private PageViewModel? currentOtherPage;

    [Reactive]
    private DialogManager dialogManager;

    [Reactive]
    private ToastManager toastManager;

    private readonly GamesService gamesService;

    public MainWindowViewModel(ToastManager tm, DialogManager dm, GamesService gs, DownloadsPageViewModel downloadsPageViewModel)
    {
        Downloads = downloadsPageViewModel;

        OtherPages.Add(Downloads);
        OtherPages.Add(new SettingsPageViewModel());

        this.WhenAnyValue(x => x.CurrentGame).Subscribe(x => OnCurrentGameChanged());
        this.WhenAnyValue(x => x.CurrentPage).Subscribe(x => OnCurrentPageChanging());
        this.WhenAnyValue(x => x.CurrentOtherPage).Subscribe(x => OnCurrentOtherPageChanged());

        Instance = this;
        toastManager = tm;
        dialogManager = dm;
        gamesService = gs;

        LoadGames();
    }

    public void LoadGames()
    {
        foreach (var game in gamesService.Games)
        {
            var gv = new GamePageViewModel(game);
            Games.Add(gv);
            Pages.Add(gv);
        }

        if (Games.Count > 0)
        {
            CurrentGame = Games[0];
        }
    }

    public void OnCurrentPageChanging()
    {
        if (CurrentPage != null)
        {
            CurrentPage.OnPageOpened();
        }
    }

    public void OnCurrentGameChanged()
    {
        if (CurrentGame != null)
        {
            CurrentOtherPage = null;
            CurrentPage = CurrentGame;
        }
    }

    public void OnCurrentOtherPageChanged()
    {
        if (CurrentOtherPage != null)
        {
            CurrentGame = null;
            CurrentPage = CurrentOtherPage;
        }
    }

    public void SetCurrentPage(string pageName)
    {
        foreach (var page in OtherPages)
        {
            if (page.Name == pageName)
            {
                CurrentOtherPage = page;
                return;
            }
        }

        foreach (var page in Games)
        {
            if (page.Name == pageName)
            {
                CurrentOtherPage = page;
            }
        }
    }
}
