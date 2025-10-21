#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text;
using T3.Core.Resource;
using T3.Editor.Gui.Windows.SymbolLib;
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.Windows.AssetLib;

/// <summary>
/// A nested container that can contain further instances of <see cref="AssetFolder"/>
/// Used to structure the <see cref="SymbolLibrary"/>.
/// </summary>
internal sealed class AssetFolder
{
    internal string Name { get; private set; }
    internal List<AssetFolder> SubFolders { get; } = [];
    private AssetFolder? Parent { get; }

    internal enum FolderTypes
    {
        ProjectNameSpace,
        Project,
        Directory
    }

    internal FolderTypes FolderType;
    
    internal AssetFolder(string name, AssetFolder? parent = null)
    {
        Name = name;
        Parent = parent;
    }

    internal string GetAliasPath()
    {
        // estimate capacity if you can
        var sb = new StringBuilder();

        var stack = new Stack<string>();
        var t = this;
        while (t != null)
        {
            stack.Push(t.Name);
            t = t.Parent;
        }

        bool first = true;
        while (stack.Count > 0)
        {
            if (!first)
                sb.Append(ResourceManager.PathSeparator);
            first = false;
            sb.Append(stack.Pop());
        }

        return sb.ToString();
    }

    internal bool TryGetAbsolutePath(out string path)
    {
        return ResourceManager.TryResolvePath(GetAliasPath(), null, out path, out _, isFolder:true);
    }

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
    internal static void PopulateCompleteTree(AssetFolder root, Predicate<AssetItem>? filterAction, List<AssetItem> allAssetsOrdered)
    {
        root.Name = RootNodeId;
        root.Clear();

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
            
            root.SortInAssets(file);
        }
    }

    /// <summary>
    /// Build up folder structure by sorting in one asset at a time
    /// creating required sub folders on the way.
    /// </summary>
    private void SortInAssets(AssetItem assetItem)
    {
        // Roll out recursion
        var currentFolder = this;
        var expandingSubTree = false;
        
        foreach (var pathPart in assetItem.FilePathFolders)
        {
            if (string.IsNullOrEmpty(pathPart))
                continue;
        
            if (!expandingSubTree)
            {
                if(currentFolder.TryGetSubFolder(pathPart, out var folder))
                {
                    currentFolder = folder;
                }
                else
                {
                    expandingSubTree = true;
                }
            }
        
            if (!expandingSubTree)
                continue;
        
            var newFolderNode = new AssetFolder(pathPart, currentFolder);
            currentFolder.SubFolders.Add(newFolderNode);
            currentFolder = newFolderNode;
        }
        
        currentFolder.FolderAssets.Add(assetItem);
    }

    private bool TryGetSubFolder(string folderName, [NotNullWhen(true)]out  AssetFolder? subFolder)
    {
        subFolder=SubFolders.FirstOrDefault(n => n.Name == folderName);
        return subFolder != null;
    }

    internal readonly List<AssetItem> FolderAssets = [];
    internal const string RootNodeId = "root";
}