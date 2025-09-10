using Avalonia.Platform;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using MWSManager.Models.Games;
using MWSManager.Models.Providers;
using MWSManager.ViewModels;
using Newtonsoft.Json;
using NexusMods.Paths;
using ReactiveUI;
using Serilog;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWSManager.Services;

public class GamesService : BaseService
{
    public override void Setup()
    {
        LoadGames();
    }

    public bool Loaded { get; private set; } = false;

    public List<Game> Games { get; private set; } = [];

    private void LoadGames()
    {
        var steamHandler = new SteamHandler(FileSystem.Shared, OperatingSystem.IsWindows() ? WindowsRegistry.Shared : null);
        var epicHandler = new EGSHandler(WindowsRegistry.Shared, FileSystem.Shared);

        var fs = new StreamReader(AssetLoader.Open(new Uri("avares://MWSManager/Assets/games.json")));
        var jsonConfig = fs.ReadToEnd();
        var data = JsonConvert.DeserializeObject<Dictionary<string, GameDefinition>>(jsonConfig);

        Log.Information("Loading Games");

        if (data != null)
        {
            foreach (var kv in data)
            {
                var gameDef = kv.Value;

                IGame? game = null;
                string gamePath = "";
                string gameName = gameDef.Name;

                var errs = new ErrorMessage[10];

                if (gameDef.SteamId != null)
                {
                    var steamGame = steamHandler.FindOneGameById(AppId.From((uint)gameDef.SteamId), out errs);
                    if (steamGame != null)
                    {
                        gamePath = steamGame.Path.GetFullPath();
                        //gameName = steamGame.Name;
                    }

                    game = steamGame;
                }

                // TODO: show warning if the user has both/how do we handle it?
                if (gameDef.EpicId != null && game == null)
                {
                    var egsGame = epicHandler.FindOneGameById(EGSGameId.From(gameDef.EpicId), out errs);
                    if (egsGame != null)
                    {
                        gamePath = egsGame.InstallLocation.GetFullPath();
                        //gameName = egsGame.DisplayName; // Why the fuck is it inconsistent???
                    }

                    game = egsGame;
                }

                if (game == null)
                {
                    continue;
                }

                Game? gameObj = null;
                if (gameDef.ClassName != null)
                {
                    var t = Type.GetType("MWSManager.Models.Games." + gameDef.ClassName);
                    if (t != null)
                    {
                        gameObj = (Game?)Activator.CreateInstance(t, gameName, gamePath, gameDef.ExtraData);
                    }
                    else
                    {
                        Log.Error($"couldn't find {gameDef.ClassName}");
                    }
                }
                else
                {
                    gameObj = new Game(gameName, gamePath, gameDef.ExtraData);
                }


                if (gameObj != null)
                {
                    if (gameDef.SpecialPaths != null)
                    {
                        foreach (var pair in gameDef.SpecialPaths)
                        {
                            Log.Information("{0} = {1}", pair.Key, pair.Value);
                            gameObj.AddSpecialPath(pair.Key, pair.Value);
                        }
                    }
                    gameObj.Thumbnail = gameDef.Icon;
                    gameObj.MWSId = gameDef.MWSId;

                    if (gameDef.ModDirs != null)
                        gameObj.ModDirs.AddRange(gameDef.ModDirs);

                    gameObj.LookForMods();
                    Games.Add(gameObj);
                }
                else
                {
                    Log.Error("Couldn't Create Game {0}", kv.Key);
                }
            }
        }

        Loaded = true;
    }
}
