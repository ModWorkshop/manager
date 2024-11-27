using MWSManager.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using Serilog;
using System.Reflection;

namespace MWSManager.Models.Games
{
    public class UnityEngineGame : Game
    {
        public UnityEngineGame(string name, string gamePath, dynamic? extraData = null) : base(name, gamePath)
        {
            ModFileDirs.Add("BepInEx/plugins/*.dll");
        }

        protected override void ProcessMod(Mod mod)
        {
            try
            {
                var plugin = Assembly.LoadFrom(mod.ModPath);
                foreach(var type in plugin.GetTypes())
                {
                    var attrs = (BepInPlugin?)type.GetCustomAttribute(typeof(BepInPlugin));
                    if (attrs != null)
                    {
                        mod.Name = attrs.Name;
                        mod.Version = attrs.Version.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Could not read DLL file: {0}", e);
            }
        }

        public override bool CheckPossibleModInNode(PathNode node, List<Mod> Mods)
        {
            if (node.IsFile && Path.GetExtension(node.Name) == ".dll")
            {
                Mods.Add(new Mod(this, node.FullPath, "BepInEx/plugins"));
                return true;
            }

            return false;
        }
    }
}
