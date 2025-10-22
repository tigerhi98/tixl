#nullable enable
using System.IO;
using T3.Core.Resource;

namespace T3.Editor.Gui.Windows.AssetLib;

internal sealed class AssetItem
{
    public required string FileAliasPath;
    public IResourcePackage? Package;
    public required FileInfo FileInfo;
    public required List<string> FilePathFolders;
    public required string PackageName;
    public required string AbsolutePath;
}