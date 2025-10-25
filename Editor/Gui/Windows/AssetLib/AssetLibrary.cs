#nullable enable

using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Helpers;
using T3.Editor.UiModel.ProjectHandling;
using T3.Editor.UiModel.Selection;

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

    /// <summary>
    /// List of extensions than can be opened by selected operator
    /// </summary>
    internal  static List<int> CompatibleExtensionIds = [];
    
    
    protected override void DrawContent()
    {
        // Init current frame
        UpdateAssetsIfRequired();
        
        if (NodeSelection.TryGetSelectedInstanceOrInput(out var selectedInstance, out _, out var selectionChanged))
        {
            selectionChanged = selectedInstance != _selectedInstance; 
            
            if (selectionChanged)
            {
                _selectedInstance = selectedInstance;
                CompatibleExtensionIds.Clear();
                Log.Debug("@@@ Selection changed");
                
                _activePathInput = null;
                _activeAbsolutePath = null;
            
                var symbolUi = _selectedInstance.GetSymbolUi();

                foreach (var input in _selectedInstance.Inputs)
                {
                    if (input is not InputSlot<string> stringInput)
                        continue;

                    var inputUi = symbolUi.InputUis[input.Id];
                    if (inputUi is not StringInputUi { Usage: StringInputUi.UsageType.FilePath } stringInputUi)
                        continue;

                    // Found a file path input in selected op
                    _activePathInput = stringInput;

                    FileExtensionRegistry.IdsFromFileFilter(stringInputUi.FileFilter, ref CompatibleExtensionIds);
                    var sb = new StringBuilder();
                    foreach (var id in CompatibleExtensionIds)
                    {
                        if (FileExtensionRegistry.TryGetExtensionForId(id, out var ext))
                        {
                            sb.Append(ext);
                            sb.Append(", ");
                        }
                        else
                        {
                            sb.Append($"#{id}");
                        }
                    }
                    Log.Debug("matching extensions " + sb);
                    
                
                    var filePath = _activePathInput.GetCurrentValue();
                    var valid = ResourceManager.TryResolvePath(filePath, _selectedInstance, out _activeAbsolutePath, out _);
                    if (!valid)
                    {
                        //Log.Debug("Active path: " + _activeAbsolutePath);
                    }
                
                    break; // only take first file path
                }
            }
        }
        
        // Draw
        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 10);
        DrawLibContent();
        ImGui.PopStyleVar(1);
    }
    
    
    private void UpdateAssetsIfRequired()
    {
        var compositionInstance = ProjectView.Focused?.CompositionInstance;
        if (compositionInstance == null)
            return;

        if (_lastFileWatcherState == ResourceFileWatcher.FileStateChangeCounter 
            && !HasObjectChanged(compositionInstance, ref _lastCompositionObjId))
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

                var fileInfo = new FileInfo(absolutePath);
                var fileInfoExtension = fileInfo.Extension.Length < 1 ? string.Empty : fileInfo.Extension[1..];
                asset = new AssetItem
                            {
                                FileAliasPath = aliasedPath,
                                FileInfo = fileInfo,
                                Package = package,
                                PackageName = packageName,
                                FilePathFolders = folders,
                                AbsolutePath = absolutePath, // With forward slashes
                                FileExtensionId = FileExtensionRegistry.GetId(fileInfoExtension)
                            };
                _assetCache[aliasedPath] = asset;
            }

            _allAssets.Add(asset);
        }
        
        AssetFolder.PopulateCompleteTree(_rootNode, compositionInstance, filterAction:null,_allAssets);
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

    private readonly AssetFolder _rootNode = new(AssetFolder.RootNodeId, null);
    private readonly SymbolFilter _filter = new();

    private readonly List<AssetItem> _allAssets = [];
    private int _lastFileWatcherState = -1;

    private readonly Dictionary<string, AssetItem> _assetCache = [];
    private bool _openedLibFolderOnce;
    private Instance? _selectedInstance;
    private InputSlot<string>? _activePathInput;
    private string? _activeAbsolutePath;
}