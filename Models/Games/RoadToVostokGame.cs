using MWSManager.Structures;
using PeanutButter.INI;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWSManager.Models.Games
{
    public class RoadToVostokGame : Game
    {
        //TODO: this game has a demo but may release soon, we should probably handle multiple Steam IDs somehow
        public RoadToVostokGame(string name, string path, dynamic? extraData = null) : base(name, path)
        {
            ModFileDirs = ["mods/*.zip"];
            SpecialPaths.Add("MODS", "mods");
        }

        protected override void ProcessMod(Mod mod)
        {
            using var fs = File.OpenRead(mod.ModPath);
            var factory = ArchiveFactory.Open(fs);
            var modTxt = factory.Entries.FirstOrDefault(entry => entry.Key == "mod.txt");
            var mwsManager = factory.Entries.FirstOrDefault(entry => entry.Key == "mws-manager.json");

            if (mwsManager != null)
            {
                mod.LoadMetadataFromString(new StreamReader(mwsManager.OpenEntryStream()).ReadToEnd());
            }
            else
            {
                var sr = new StreamReader(modTxt.OpenEntryStream());
                var cfg = INIFile.FromString(sr.ReadToEnd());
                mod.Name ??= cfg.GetValue("mod", "name");
                mod.Version ??= cfg.GetValue("mod", "version");
                var mwsUpdateId = cfg.GetValue("updates", "modworkshop");
                if (mwsUpdateId != null)
                {
                    mod.Updates.Add(new ModUpdate(mod, "mws", mwsUpdateId, mod.Version));
                }
            }

        }

        // Extract only if it has a mod.txt as expected
        public override bool ShouldExtract(string fileName, Stream stream)
        {
            var factory = ArchiveFactory.Open(stream);
            foreach (var entry in factory.Entries)
            {
                if (entry.Key == "mod.txt")
                {
                    return false;
                }
            }

            return true;
        }

        //TODO: this is basically the same as CassetteBeasts except it uses zip files, maybe we should do this automatically in some cases
        public override bool CheckPossibleModInNode(PathNode node, List<Mod> Mods)
        {
            // Detects psk files and installs them in the mods folder
            if (node.IsFile && Path.GetExtension(node.Name) == ".zip")
            {
                Mods.Add(new Mod(this, node.FullPath, "mods"));
                return true;
            }

            return false;
        }
    }
}
