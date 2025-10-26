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
        _state.Filter.SearchString = "";
        Config.Title = "Assets";
    }

    internal override List<Window> GetInstances()
    {
        return [];
    }

    protected override void DrawContent()
    {
        // Init current frame
        UpdateAssetsIfRequired();
        if (_state.Composition == null)
            return;
        

        if (!NodeSelection.TryGetSelectedInstanceOrInput(out var selectedInstance, out _, out var selectionChanged))
        {
            selectedInstance = _state.Composition;
        }

        UpdateActiveSelection(selectedInstance);

        // Draw
        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 10);
        DrawLibContent();
        ImGui.PopStyleVar(1);
    }


    private void UpdateAssetsIfRequired()
    {
        _state.Composition = ProjectView.Focused?.CompositionInstance;
        if (_state.Composition == null)
            return;

        if (_state.LastFileWatcherState == ResourceFileWatcher.FileStateChangeCounter
            && !HasObjectChanged(_state.Composition, ref _lastCompositionObjId))
            return;

        _state.TreeHandler.Reset();
        _state.LastFileWatcherState = ResourceFileWatcher.FileStateChangeCounter;

        _state.AllAssets.Clear();
        var filePaths = ResourceManager.EnumerateResources([],
                                                           isFolder: false,
                                                           _state.Composition.AvailableResourcePackages,
                                                           ResourceManager.PathMode.Aliased);

        foreach (var aliasedPath in filePaths)
        {
            if (!_state.AssetCache.TryGetValue(aliasedPath, out var asset))
            {
                if (!ResourceManager.TryResolvePath(aliasedPath, _state.Composition, out var absolutePath, out var package))
                {
                    Log.Warning($"Can't find file {aliasedPath}");
                    continue;
                }

                ParsePath(aliasedPath, out var packageName, out var folders);

                var fileInfo = new FileInfo(absolutePath);
                var fileInfoExtension = fileInfo.Extension.Length < 1 ? string.Empty : fileInfo.Extension[1..];
                var fileExtensionId = FileExtensionRegistry.GetId(fileInfoExtension);
                if (!AssetTypeRegistry.TryGetFromId(fileExtensionId, out var assetType))
                {
                    Log.Warning($"Can't fine file type for: {fileInfoExtension}");
                }

                asset = new AssetItem
                            {
                                FileAliasPath = aliasedPath,
                                FileInfo = fileInfo,
                                Package = package,
                                PackageName = packageName,
                                FilePathFolders = folders,
                                AbsolutePath = absolutePath, // With forward slashes
                                FileExtensionId = fileExtensionId,
                                AssetType = assetType,
                            };
                _state.AssetCache[aliasedPath] = asset;
            }

            _state.AllAssets.Add(asset);
        }

        AssetFolder.PopulateCompleteTree(_state, filterAction: null);
    }

    private static void ParsePath(string path, out string package, out List<string> folders)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        package = parts.Length > 0 ? parts[0] : string.Empty;
        folders = parts.Length > 2
                      ? parts[0..^1].ToList()
                      : [];
    }

    
    private void UpdateActiveSelection(Instance selectedInstance)
    {
        _state.HasActiveInstanceChanged = selectedInstance != _state.ActiveInstance;
        if (!_state.HasActiveInstanceChanged)
            return;

        _state.TimeActiveInstanceChanged = ImGui.GetTime();
        
        _state.ActiveInstance = selectedInstance;
        _state.ActivePathInput = null;
        _state.ActiveAbsolutePath = null;
        
        AssetLibState.CompatibleExtensionIds.Clear();
        
        // Check if active instance has asset reference...
        var symbolUi = _state.ActiveInstance.GetSymbolUi();
        foreach (var input in _state.ActiveInstance.Inputs)
        {
            if (input is not InputSlot<string> stringInput)
                continue;

            var inputUi = symbolUi.InputUis[input.Id];
            if (inputUi is not StringInputUi { Usage: StringInputUi.UsageType.FilePath } stringInputUi)
                continue;

            // Found a file path input in selected op
            _state.ActivePathInput = stringInput;

            FileExtensionRegistry.IdsFromFileFilter(stringInputUi.FileFilter, ref AssetLibState.CompatibleExtensionIds);
            var sb = new StringBuilder();
            foreach (var id in AssetLibState.CompatibleExtensionIds)
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

            var filePath = _state.ActivePathInput?.GetCurrentValue();
            var valid = ResourceManager.TryResolvePath(filePath, _state.ActiveInstance, out _state.ActiveAbsolutePath, out _);
            if (!valid)
            {
                //Log.Debug("Active path: " + _activeAbsolutePath);
            }

            return; // only take first file path
        }
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

    private AssetLibState _state = new();
}