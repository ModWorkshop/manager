﻿using Newtonsoft.Json;
using SharpCompress.Common;
using SharpCompress.Readers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MWSManager.Models.Diesel
{
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

    public class PD2Game : Game
    {
        public PD2Game(string name, string path, dynamic? extraData = null) : base(name, path, extraData)
        {
            IgnoreModNames.Add("logs");
            IgnoreModNames.Add("downloads");
            IgnoreModNames.Add("saves");
        }

        // Processes the mod, tries to load data like mod.txt and such to figure name, version, etc.
        protected override void ProcessMod(Mod mod, string modDir)
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
                        {
                            mod.Name = data.name;
                        }

                        if (data.version != null)
                        {
                            mod.Version = data.version;
                        }

                        if (data.author != null)
                        {
                            mod.Owner = data.author;
                        }

                        if (data.image != null)
                        {
                            var ext = Path.GetExtension(data.image);
                            // TODO: possibly handle images ourselves, sadly the async image loader doesn't handle invalid data
                            // Give it something invalid and it will crash the program..
                            if (ext == ".png")
                            {
                                mod.Thumbnail = $"{mod.ModPath}/{data.image}";
                            }
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
                                if (reader.Name == "mod" || reader.Name == "table") {
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

        protected override string GetModInstallPath(ModInstall install)
        {
            foreach(var filePath in install.Files)
            {
                var fileName = Path.GetFileName(filePath);

                // BLT mods are automatically to be installed in mods folder!
                if (fileName == "supermod.xml" || fileName == "mod.txt")
                {
                    return "mods";
                }

                // Maps should be installed in Maps folder
                if (fileName == "add_local.xml")
                {
                    return "Maps";
                }
            }

            return "assets/mod_overrides";
        }
    }
}
