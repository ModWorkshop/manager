using Avalonia.Logging;
using Avalonia.Markup.Xaml.Templates;
using MWSManager.Models.Games;
using MWSManager.Services;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWSManager.Models
{
    public partial class Mod : ReactiveObject
    {
        #region Properties
        private string? name;
        /// <summary>
        /// The name of the mod. If none is set, defaults to the folder/file name of the ModPath
        /// </summary>
        public string? Name { 
            get => name ?? Path.GetFileNameWithoutExtension(ModPath);
            set => this.RaiseAndSetIfChanged(ref name, value);
        }

        /// <summary>
        /// The authors of the mod
        /// </summary>
        public List<string> Authors { get; set; } = [];

        /// <summary>
        /// The ID of the mod in MWS (optional, used for stuff like page buttons)
        /// </summary>
        public int? Id;

        /// <summary>
        /// The version of the mod
        /// </summary>
        [Reactive]
        public string? version;

        [Reactive]
        public string? desc;

        /// <summary>
        /// The version of the mod
        /// </summary>
        public string? ModPath { get; set; }

        /// <summary>
        /// URL or path to the image of the mod
        /// </summary>
        public string? Thumbnail { get; set; }

        /// <summary>
        /// Whether or not the mod is a single file such as a pak file. Not recommended as you won't be able to load a metadata file.
        /// </summary>
        public bool IsFile { get; set; }

        /// <summary>
        /// Whether or not metadata file has been loaded
        /// </summary>
        public bool LoadedMetadata { get; private set; } = false;

        /// <summary>
        /// Each update that the mod supports.
        /// Planned mostly to be mirrors, will likely be different for mod components that have their own updates.
        /// </summary>
        public ObservableCollection<ModUpdate> Updates { get; } = [];

        /// <summary>
        /// The directory that the mod was installed in. This is almost always the directory of ModPath (Without the game directory)
        /// </summary>
        public string? InstallDir { get; set; }

        /// <summary>
        /// The game the mod belongs to
        /// </summary>
        public Game Game { get; set; }

        #endregion

        public Mod(Game game, string modPath, string? installDir = null)
        {
            Game = game;

            FileAttributes attrs = File.GetAttributes(modPath);
            IsFile = !attrs.HasFlag(FileAttributes.Directory);

            ModPath = PathUtils.NormalizePath(modPath);
            InstallDir = installDir;

            LoadMetadata();
        }

        /// <summary>
        /// Loads metadataw automatically from ModPath
        /// </summary>
        /// <param name="reset">Whether or not to reset the previous loaded data. Useful for updates.</param>
        public void LoadMetadata(bool reset = false)
        {
            if (reset)
            {
                Name = null;
                Version = null;
                Authors = [];
                LoadedMetadata = false;
                Updates.Clear();
            }

            if (ModPath != null)
            {
                if (IsFile)
                {
                    var metadataPath = Path.Combine(Path.GetDirectoryName(ModPath), $"{Path.GetFileNameWithoutExtension(ModPath)}.mws-manager.json");

                    if (File.Exists(metadataPath))
                    {
                        LoadMetadataFromString(File.ReadAllText(metadataPath));
                    }
                } else
                {
                    var metadataPath = Path.Combine(ModPath, "mws-manager.json");

                    if (File.Exists(metadataPath))
                    {
                        LoadMetadataFromString(File.ReadAllText(metadataPath));
                    }
                }
            }
        }

        /// <summary>
        /// Loads a mod-manager.json file from a string. This will override some info you might've already defined.
        /// </summary>
        /// <param name="json">The mod's metadata JSON string</param>
        public void LoadMetadataFromString(string json)
        {
            var metadata = JsonConvert.DeserializeObject<ModMetadata>(json);
            if (metadata != null)
            {
                LoadedMetadata = true;

                Name = metadata.name;
                Desc = metadata.desc;
                Version = metadata.version;
                Authors = [.. metadata.authors];
                Id = metadata.id;

                if (metadata.thumbnail != null && ModPath != null)
                    Thumbnail = Path.Combine(ModPath, metadata.thumbnail);

                if (metadata.installDir != null)
                {
                    InstallDir = Game.ParsePath(metadata.installDir);
                }

                foreach (var up in metadata.updates)
                {
                    Updates.Add(new ModUpdate(this, up.provider, up.id, Version));
                }
            }
        }


        /// <summary>
        /// Moves the mod into a different directory
        /// </summary>
        public bool Move(string dir, bool prefixGamePath = true)
        {
            if (ModPath == null)
            {
                throw new Exception("Tried to move a mod without a mod path!");
            }

            if (prefixGamePath)
            {
                dir = Path.Combine(Game.GamePath, dir);
            }

            try
            {
                var oldModPath = ModPath;
                ModPath = Path.Combine(dir, Path.GetFileName(ModPath));
                if (IsFile)
                {
                    Log.Information("Move File {0} -> {1}", oldModPath, ModPath);
                    File.Move(oldModPath, ModPath);
                    if (LoadedMetadata)
                    {
                        var metadataFile = $"{Path.GetFileNameWithoutExtension(ModPath)}.mws-manager.json";
                        File.Move(Path.Combine(Path.GetDirectoryName(ModPath), metadataFile), Path.Combine(dir, metadataFile));
                    }
                }
                else
                {
                    Log.Information("Move {0} -> {1}", oldModPath, ModPath);
                    Utils.CopyDirectory(oldModPath, ModPath);
                    Directory.Delete(oldModPath, true);
                }
                Log.Information("Move Success");
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Couldn't move: {0}", e);
            }
            return false;
        }

        public void Delete()
        {
            if (ModPath == null)
                return;

            if (IsFile)
            {
                File.Delete(ModPath);
                if (LoadedMetadata)
                {
                    File.Delete(Path.Combine(Path.GetDirectoryName(ModPath), $"{Path.GetFileNameWithoutExtension(ModPath)}.mws-manager.json"));
                }
            } else
            {
                Directory.Delete(ModPath, true);
            }

            Unregister();
        }

        public void Register()
        {
            Game.AddMod(this);

            var updatesService = Locator.Current.GetService<UpdatesService>()!;

            foreach (var update in Updates)
            {
                updatesService.AddUpdate(update);
            }
        }

        public void Unregister()
        {
            Game.RemoveMod(this);

            var updatesService = Locator.Current.GetService<UpdatesService>()!;

            foreach (var update in Updates)
            {
                updatesService.RemoveUpdate(update);
            }
        }
    }
}
