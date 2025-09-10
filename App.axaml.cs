using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MWSManager.Models;
using MWSManager.ViewModels;
using MWSManager.Views;
using System;
using System.Diagnostics;
using URIScheme;
using System.Reflection;
using System.Threading;
using System.Text.RegularExpressions;
using System.Linq;
using MWSManager.Services;
using Avalonia.Threading;
using Avalonia.Controls;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Serilog;
using System.IO;
using Splat;
using ShadUI;
using System.Collections.Generic;

namespace MWSManager;

public partial class App : Application
{
    [DllImport("Kernel32")]
    public static extern void AllocConsole();

    [DllImport("Kernel32")]
    public static extern void FreeConsole();

    public static MainWindowViewModel MainWindowViewModelService => Locator.Current.GetService<MainWindowViewModel>()!;

    private List<BaseService> Services = [];

    private UpdatesService updatesService => Locator.Current.GetService<UpdatesService>()!;

    public override void Initialize()
    {
        //if (File.Exists("developer.txt"))
        //{
            AllocConsole();
        //}
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("log.txt")
            .CreateLogger();

        Log.Information("Loading App");

        var service = URISchemeServiceFactory.GetURISchemeSerivce("mws-manager", "URI Scheme for the MWS Manager", Process.GetCurrentProcess().MainModule.FileName);
        if (service.CheckAny())
        {
            service.Delete();
        }

        service.Set();

        AvaloniaXamlLoader.Load(this);

        var build = Locator.CurrentMutable;
        //build.RegisterLazySingleton(() => (IDialogService)new DialogService(
        //    new DialogManager(
        //        viewLocator: new ViewLocator(),
        //        dialogFactory: new DialogFactory().AddFluent(FluentMessageBoxType.TaskDialog)),
        //    viewModelFactory: x => Locator.Current.GetService(x)));

        build.RegisterLazySingleton(() => new DialogManager());
        build.RegisterLazySingleton(() => new ToastManager());
        build.RegisterConstant(new GamesService());
        build.RegisterConstant(new UpdatesService());

        Services.Add(Locator.Current.GetService<GamesService>()!);
        Services.Add(Locator.Current.GetService<UpdatesService>()!);
        foreach (var s in Services)
        {
            s.Setup();
        }

        SplatRegistrations.Register<DownloadsPageViewModel>();
        SplatRegistrations.Register<MainWindowViewModel>();
        SplatRegistrations.SetupIOC();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = MainWindowViewModelService,
            };
        }

        base.OnFrameworkInitializationCompleted();

        string[] args = Environment.GetCommandLineArgs();
        if (args.Length > 2)
        {
            if (args[1] != null)
            {
                OnMessage(args[1]);
            }

        }
    }

    public void OnMessage(string message)
    {
        if (message != null) {
            var match = Regex.Match(message, @"mws-manager://([-_a-zA-Z0-9]+)/(\w+)/([-_a-zA-Z0-9]+)");
            var provider = match.Groups[1];
            var action = match.Groups[2];
            var id = match.Groups[3];

            if (provider != null && action != null && id != null)
            {
                Utils.RunInUI(() => updatesService.URISchemeHandle(provider.Value, action.Value, id.Value));
            }
        }
    }
}