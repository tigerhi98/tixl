#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text;
using T3.Core.Operator;
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

    /// <summary>
    /// This could later be used for UI to distinguish projects from folders 
    /// </summary>
    internal FolderTypes FolderType;

    internal enum FolderTypes
    {
        ProjectNameSpace,
        Project,
        Directory
    }

    internal readonly string AbsolutePath;
    internal readonly string AliasPath;

    internal AssetFolder(string name, Instance? selectedInstance, AssetFolder? parent = null, FolderTypes type = FolderTypes.Directory)
    {
        Name = name;
        Parent = parent;
        FolderType = type;

        AliasPath = GetAliasPath();
        if (!ResourceManager.TryResolvePath(AliasPath, selectedInstance, out AbsolutePath, out _, isFolder: true))
        {
            Log.Warning($"Can't resolve folder path ? {AliasPath}");
        }
    }

    // Define an action delegate that takes a Symbol and returns a bool
    internal static void PopulateCompleteTree(AssetFolder root, Instance? composition, Predicate<AssetItem>? filterAction, List<AssetItem> allAssetsOrdered)
    {
        if (composition == null)
            return;

        root.Name = RootNodeId;
        root.Clear();

        foreach (var file in allAssetsOrdered)
        {
            var keep = filterAction == null || filterAction(file);
            if (!keep)
                continue;

            root.SortInAssets(file, composition);
        }
    }

    /// <summary>
    /// Build up folder structure by sorting in one asset at a time
    /// creating required sub folders on the way.
    /// </summary>
    private void SortInAssets(AssetItem assetItem, Instance composition)
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
                if (currentFolder.TryGetSubFolder(pathPart, out var folder))
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

            var newFolderNode = new AssetFolder(pathPart, composition, currentFolder);
            currentFolder.SubFolders.Add(newFolderNode);
            currentFolder = newFolderNode;
        }

        currentFolder.FolderAssets.Add(assetItem);
    }

    private bool TryGetSubFolder(string folderName, [NotNullWhen(true)] out AssetFolder? subFolder)
    {
        subFolder = SubFolders.FirstOrDefault(n => n.Name == folderName);
        return subFolder != null;
    }

    private string GetAliasPath()
    {
        var sb = new StringBuilder(4);

        var stack = new Stack<string>();
        var t = this;
        while (t != null && t.Name != RootNodeId)
        {
            stack.Push(t.Name);
            t = t.Parent;
        }

        var first = true;
        while (stack.Count > 0)
        {
            if (first)
            {
                sb.Append(ResourceManager.PathSeparator);
                first = false;
            }

            sb.Append(stack.Pop());
            sb.Append(ResourceManager.PathSeparator);
        }

        return sb.ToString();
    }

    private void Clear()
    {
        SubFolders.Clear();
        FolderAssets.Clear();
    }

    internal readonly List<AssetItem> FolderAssets = [];
    internal const string RootNodeId = "__root__";
}