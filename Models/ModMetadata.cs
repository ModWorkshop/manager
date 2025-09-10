using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MWSManager.Models;

public class ModMetadataUpdate
{
    public required string provider;
    public required string id;

    public string name;
    public string folderName;
    public string installDir;
    public string versionFile;
    public bool dontDelete;
}

public class ModMetadata
{
    public string name = "Missing Name";
    public string desc = "";
    public string[] authors = [];
    public string version = "";

    public string? thumbnail;

    public string? installDir;

    public int? id;

    public ModMetadataUpdate[] updates = [];
    //TODO: dependencies?
}
