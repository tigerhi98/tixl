#nullable enable

using System.IO;
using System.Runtime.CompilerServices;
using T3.Core.Resource;
using T3.Editor.UiModel.Helpers;
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.Windows.AssetLib;

/// <summary>
/// Shows a tree of all defined symbols sorted by namespace 
/// </summary>
internal sealed partial class AssetLibrary : Window
{
    internal AssetLibrary()
    {
        _filter.SearchString = "";
        Config.Title = "Assets";
    }

    internal override List<Window> GetInstances()
    {
        return [];
    }
    

    private void UpdateAssetsIfRequired()
    {
        var compositionInstance = ProjectView.Focused?.CompositionInstance;
        if (compositionInstance == null)
            return;

        if (_lastFileWatcherState == ResourceFileWatcher.FileStateChangeCounter && !HasObjectChanged(compositionInstance, ref _lastCompositionObjId))
            return;

        _lastFileWatcherState = ResourceFileWatcher.FileStateChangeCounter;

        _allAssets.Clear();
        var filePaths = ResourceManager.EnumerateResources([],
                                                           isFolder: false,
                                                           compositionInstance.AvailableResourcePackages,
                                                           ResourceManager.PathMode.Aliased);

        foreach (var aliasedPath in filePaths)
        {
            if (!_assetCache.TryGetValue(aliasedPath, out var asset))
            {
                if (!ResourceManager.TryResolvePath(aliasedPath, compositionInstance, out var absolutePath, out var package))
                {
                    Log.Warning($"Can't find file {aliasedPath}");
                    continue;
                }

                ParsePath(aliasedPath, out var packageName, out var folders);
                
                asset = new AssetItem
                            {
                                FileAliasPath = aliasedPath,
                                FileInfo = new FileInfo(absolutePath),
                                Package = package,
                                PackageName = packageName,
                                FilePathFolders = folders,
                            };
                _assetCache[aliasedPath] = asset;
            }

            _allAssets.Add(asset);
        }
        
        AssetFolder.PopulateCompleteTree(_rootNode, filterAction:null,_allAssets);
    }

    private static void ParsePath(string path, out string package, out List<string> folders)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        package = parts.Length > 0 ? parts[0] : string.Empty;
        folders = parts.Length > 2
                      ? parts[0..^1].ToList()
                      : [];
    }
    
    /// <summary>
    /// Useful for checking if a reference has changed without keeping an GC reference. 
    /// </summary>
    private static bool HasObjectChanged(object? obj, ref int? lastObjectId)
    {
        int? id = obj is null ? null : RuntimeHelpers.GetHashCode(obj);
        if (id == lastObjectId)
            return false;

        lastObjectId = id;
        return true;
    }

    private int? _lastCompositionObjId = 0;

    private readonly AssetFolder _rootNode = new(AssetFolder.RootNodeId);
    private readonly SymbolFilter _filter = new();

    private readonly List<AssetItem> _allAssets = [];
    private int _lastFileWatcherState = -1;

    private readonly Dictionary<string, AssetItem> _assetCache = [];
    private bool _openedLibFolderOnce;
}