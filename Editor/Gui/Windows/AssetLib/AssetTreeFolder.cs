#nullable enable

using System.Diagnostics.CodeAnalysis;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Windows.SymbolLib;
using T3.Editor.UiModel;
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.Windows.AssetLib;

/// <summary>
/// A nested container that can contain further instances of <see cref="AssetTreeFolder"/>
/// Used to structure the <see cref="SymbolLibrary"/>.
/// </summary>
internal sealed class AssetTreeFolder
{
    internal string Name { get; private set; }
    internal List<AssetTreeFolder> SubFolders { get; } = [];
    private AssetTreeFolder? Parent { get; }

    internal enum FolderTypes
    {
        ProjectNameSpace,
        Project,
        Directory
    }

    internal FolderTypes FolderType;
    
    internal AssetTreeFolder(string name, AssetTreeFolder? parent = null)
    {
        Name = name;
        Parent = parent;
    }

    // internal string GetAsString()
    // {
    //     var list = new List<string>();
    //     var t = this;
    //     while (t.Parent != null)
    //     {
    //         list.Insert(0, t.Name);
    //         t = t.Parent;
    //     }
    //
    //     return string.Join(ResourceManager.PathSeparator, list);
    // }

    private void Clear()
    {
        SubFolders.Clear();
        FolderAssets.Clear();
    }

    private static readonly List<string> _rootProjectNames = [
            "Lib.",
            "Types.",
            "Examples.",
            "t3.",
        ]; 
    
    
    // Define an action delegate that takes a Symbol and returns a bool
    internal void PopulateCompleteTree(Predicate<FileAsset>? filterAction, List<FileAsset> allAssetsOrdered)
    {
        Name = RootNodeId;
        Clear();

        var compositionInstance = ProjectView.Focused?.CompositionInstance;
        if (compositionInstance == null) 
            return;
        
        // var allFiles = ResourceManager.EnumerateResources(null,
        //                                                   isFolder:false,
        //                                                   compositionInstance.AvailableResourcePackages,
        //                                                   ResourceManager.PathMode.Aliased);
        
        // var ordered = EditorSymbolPackage.AllSymbolUis
        //                                  .OrderBy(ui =>
        //                                           {
        //                                               var ns = ui.Symbol.Namespace ?? string.Empty;
        //
        //                                               // Find matching root index
        //                                               var index = _rootProjectNames.FindIndex(p => ns.StartsWith(p, StringComparison.Ordinal));
        //                                               if (index < 0)
        //                                                   index = int.MaxValue;
        //
        //                                               return (index, ns + ui.Symbol.Name);
        //                                           });        
        
        foreach (var file in allAssetsOrdered)
        {
            var keep = filterAction == null || filterAction(file);
            if (!keep)
                continue;
            
            SortInAssets(file);
        }
    }


    
    private void SortInAssets(FileAsset asset)
    {
        //if(asset.Package.RootNamespace)
        
        // if (asset.Package == null)
        // {
        //     return;
        // }
        
        //var spaces = asset.Namespace.Split('.');

        // var segments = asset.FileAliasPath.Split(ResourceManager.PathSeparator);
        // if (segments.Length < 2)
        // {
        //     Log.Warning("Invalid path " + asset.FileAliasPath);
        //     return;
        // }
        
        var currentNode = this;
        var expandingSubTree = false;
        
        foreach (var folderName in asset.Folders)
        {
            if (folderName == "")
                continue;
        
            if (!expandingSubTree)
            {
                if(currentNode.TryFindNodeDataByName(folderName, out var node))
                {
                    currentNode = node;
                }
                else
                {
                    expandingSubTree = true;
                }
            }
        
            if (!expandingSubTree)
                continue;
        
            var newNode = new AssetTreeFolder(folderName, currentNode);
            currentNode.SubFolders.Add(newNode);
            currentNode = newNode;
        }
        
        currentNode.FolderAssets.Add(asset);
    }

    private bool TryFindNodeDataByName(string name, [NotNullWhen(true)]out  AssetTreeFolder? node)
    {
        node=SubFolders.FirstOrDefault(n => n.Name == name);
        return node != null;
    }

    internal readonly List<FileAsset> FolderAssets = [];
    internal const string RootNodeId = "root";
}