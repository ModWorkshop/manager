using MWSManager.Structures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MWSManager.Models.Games.Diesel;

internal class PDTHGame(string name, string path, dynamic? extraData = null) : DieselGame(name, path)
{
    public override bool CheckPossibleModInNode(PathNode node, List<Mod> Mods)
    {
        if (base.CheckPossibleModInNode(node, Mods))
        {
            return true;
        }

        string? installDir = null;
        var modNode = node;

        if (!node.IsFile)
        {
            // BLT mods are automatically to be installed in mods folder!
            if (node.Contains("base.lua"))
            {
                installDir = "mods";
            }
        }

        if (installDir != null)
        {
            Mods.Add(new Mod(this, modNode.FullPath, installDir));
            return true;
        }

        return installDir != null;
    }
}
