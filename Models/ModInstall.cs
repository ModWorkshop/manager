using Avalonia.Controls;
using DynamicData;
using GameFinder.Common;
using MWSManager.Models.Games;
using MWSManager.Services;
using MWSManager.Structures;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWSManager.Models;

public class ModInstall
{
    static List<string> KnownArchiveTypes = ["zip", "7z", "rar", "gz", "tar", "xz", "gzip", "bzip2"];

    // The path of the mod install, generally in some temporary path
    public string TempPath { get; init; }

    public List<string> Files = [];
    public List<string> Folders = [];

    public PathNode Tree;

    public ModUpdate Update { get; }

    public Game Game => Update.Game;

    public bool SingleFile => Files.Count == 1 && Folders.Count == 0;

    public ModInstall(ModUpdate update, string fileName, Stream stream)
    {
        Update = update;

        var mwsTemp = Path.Combine(Path.GetTempPath(), "mws-manager");
        var type = Path.GetExtension(fileName).Replace(".", "");

        TempPath = Path.Combine(mwsTemp, Path.GetFileNameWithoutExtension(fileName) + "_" +  Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
        TempPath = PathUtils.NormalizePath(TempPath);

        if (!Directory.Exists(TempPath))
        {
            Directory.CreateDirectory(TempPath);
        }

        Tree = new(TempPath);

        // Generally assume that if it's an archive file it needs to be extracted.
        // It's possible we'll need to make special cases for some games. At the moment, I don't know any game that needs such things.
        if (KnownArchiveTypes.Contains(type) && update.Game.ShouldExtract(fileName, stream))
        {
            stream.Position = 0;

            var factory = ArchiveFactory.Open(stream);
            foreach(var entry in factory.Entries)
            {
                if (entry.Key != null)
                {
                    if (entry.IsDirectory)
                        Folders.Add(entry.Key);
                    else
                        Files.Add(entry.Key);

                    Tree.Add(Path.Combine(TempPath, entry.Key), !entry.IsDirectory);
                }
            }

           factory.WriteToDirectory(TempPath,
                new SharpCompress.Common.ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                }
            );
        } else
        {
            using var tempFile = File.OpenWrite(Path.Combine(TempPath, fileName));
            stream.CopyTo(tempFile);
            Files.Add(fileName);

            Tree.Add(Path.Combine(TempPath, fileName), true);
        }
    }

    public void Install()
    {
        var mods = FindModsWithMetadataFile(Tree);

        // Let the game try figuring out where to put the files
        // We don't want to mix the two though
        if (mods.Count == 0)
        {
            Log.Information("Found no mods with metadata file, trying to search with game...");
            mods = Game.FindModsInTree(Tree);
        }

        if (mods.Count == 0)
        {
            Log.Error("Unable to figure out where to install the mod!");
            return;
        }

        foreach (var mod in mods)
        {
            if (mod.InstallDir == null)
            {
                Directory.Delete(TempPath);
                throw new Exception("Attempted to install mods with invalid insall directory. Avoiding doing any action.");
            }
        }

        // Remove old mod
        if (!Update.FreshInstall)
        {
            var oldMod = Update.Mod;
            oldMod.Unregister();
            if (oldMod.IsFile)
            {
                File.Delete(Update.Mod.ModPath);
            }
            else
            {
                Directory.Delete(Update.Mod.ModPath, true);
            }
        }

        foreach (var mod in mods)
        {
            mod.Move(mod.InstallDir);
            mod.Register();
        }

        Directory.Delete(TempPath);
    }

    /// <summary>
    /// Attempts to find mods in the PathTree, returns a list of tuples that contain the mod and path to them.
    /// It looks through each child of the root and if finds none, continues recursively to the grandchildren.
    /// This prevents finding a metadata file that is contained inside another mod (mod component)
    /// </summary>
    public List<Mod> FindModsWithMetadataFile(PathNode node)
    {
        List<Mod> Mods = [];

        if (node.Count == 0)
            return Mods;

        foreach(var childNode in node.ChildNodes)
        {
            if (childNode.IsFile && childNode.Name == "mws-manager.json")
            {
                Log.Information("Found a mod: {0}", childNode.Parent!.FullPath);
                Mods.Add(new Mod(Game, childNode.Parent!.FullPath));
            }
        }

        foreach (var childNode in node.ChildNodes)
        {
            Mods.AddRange(FindModsWithMetadataFile(childNode));
        }

        return Mods;
    }

    public string GetRealPath(string path)
    {
        return Path.Combine(TempPath, path);
    }
}
