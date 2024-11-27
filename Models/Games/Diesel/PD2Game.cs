using MWSManager.Structures;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;


namespace MWSManager.Models.Games.Diesel;

// mod.txt definition file
public class BLTModDefinition
{
    public string? name;
    public string? description;
    public string? author;
    public string? version;
    public string? image;
}

public class BeardLibMain
{
    public string? name;
}

public class PD2Game : DieselGame
{
    public PD2Game(string name, string path, dynamic? extraData = null) : base(name, path)
    {
        ModDirs.Add("Maps");
        SpecialPaths.Add("MAPS", "Maps");

        IgnoreModNames.Add("logs");
        IgnoreModNames.Add("downloads");
        IgnoreModNames.Add("saves");
    }

    // Processes the mod, tries to load data like mod.txt and such to figure name, version, etc.
    protected override void ProcessMod(Mod mod)
    {
        LoadBLTMod(mod);
        LoadBeardLibMod(mod);
    }

    private void LoadBLTMod(Mod mod)
    {
        var modDefinition = $"{mod.ModPath}/mod.txt";

        if (File.Exists(modDefinition))
        {
            var jsonText = File.ReadAllText(modDefinition);
            try
            {
                var data = JsonConvert.DeserializeObject<BLTModDefinition>(jsonText);

                if (data != null)
                {
                    if (data.name != null)
                        mod.Name = data.name;

                    if (data.version != null)
                        mod.Version = data.version;

                    if (data.author != null && (!mod.LoadedMetadata || mod.Authors.Count == 0)) // Avoid duplicates
                        mod.Authors.Add(data.author);

                    if (data.image != null && mod.Thumbnail == null)
                    {
                        var ext = Path.GetExtension(data.image);
                        // TODO: possibly handle images ourselves, sadly the async image loader doesn't handle invalid data
                        // Give it something invalid and it will crash the program..
                        if (ext == ".png")
                            mod.Thumbnail = $"{mod.ModPath}/{data.image}";
                    }
                }
            }
            catch (Exception e)
            {
                //Trace.TraceError(e.Message);
            }
        }
    }

    private void LoadBeardLibMod(Mod mod)
    {
        var mainFile = $"{mod.ModPath}/main.xml";

        if (File.Exists(mainFile))
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            // Required for the mess known as custom xml lol
            settings.ConformanceLevel = ConformanceLevel.Fragment;

            using var stream = new FileStream(mainFile, FileMode.Open);
            using var reader = XmlReader.Create(stream, settings);

            while (reader.Read())
            {
                bool found = false;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        {
                            if (reader.Name == "mod" || reader.Name == "table")
                            {
                                //Trace.WriteLine("Name: "+ reader.GetAttribute("name"));
                            }
                            else if (reader.Name == "AssetUpdates")
                            {
                                //Trace.WriteLine(reader.GetAttribute("id"));
                                found = true;
                            }
                            break;
                        }
                }
                if (found)
                {
                    break;
                }
            }
        }
    }

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
            if (node.Contains("mod.txt") || node.Contains("supermod.xml"))
            {
                installDir = "mods";
            }
            else if (node.Contains("main.xml"))
            {
                // Note: This will install maps in mod_overrides, but considering BeardLib 5, this should be fine
                // Maps folder now is mostly if you wanna edit maps, otherwise it makes no difference where you put it iirc.
                installDir = ModOverridesDir;
            }
        }

        if (installDir != null)
        {
            Mods.Add(new Mod(this, modNode.FullPath, installDir));
            return true;
        }

        return false;
    }
}
