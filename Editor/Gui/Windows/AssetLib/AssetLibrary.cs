#nullable enable

using System.IO;
using System.Runtime.CompilerServices;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Windows.SymbolLib;
using T3.Editor.UiModel.Helpers;
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.Windows.AssetLib;

/// <summary>
/// Shows a tree of all defined symbols sorted by namespace 
/// </summary>
internal sealed class AssetLibrary : Window
{
    internal AssetLibrary()
    {
        _filter.SearchString = "";
        Config.Title = "Assets";
        _treeNode.PopulateCompleteTree();
    }

    internal override List<Window> GetInstances()
    {
        return [];
    }

    protected override void DrawContent()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 10);

        DrawView();

        ImGui.PopStyleVar(1);
    }

    private void DrawView()
    {
        var iconCount = 1;

        CustomComponents.DrawInputFieldWithPlaceholder("Search symbols...", ref _filter.SearchString, -ImGui.GetFrameHeight() * iconCount + 16);

        ImGui.BeginChild("scrolling", Vector2.Zero, false, ImGuiWindowFlags.NoBackground);
        {
            UpdateAssetsIfRequired();

            foreach (var fa in _allAssets)
            {
                ImGui.TextUnformatted(fa.FileAliasPath);
                ImGui.SameLine(0, 20);
                ImGui.TextUnformatted("" + fa.FileInfo.Length);
            }
        }
        ImGui.EndChild();
    }

    // private void DrawFilteredList()
    // {
    //     _filter.UpdateIfNecessary(null);
    //     foreach (var symbolUi in _filter.MatchingSymbolUis)
    //     {
    //         DrawAssetItem(symbolUi.Symbol);
    //     }
    // }

    private void DrawFolder(NamespaceTreeNode subtree)
    {
        // if (subtree.Name == NamespaceTreeNode.RootNodeId)
        // {
        //     DrawNodeItems(subtree);
        // }
        // else
        // {
        //     ImGui.PushID(subtree.Name);
        //     ImGui.SetNextItemWidth(10);
        //     if (subtree.Name == "Lib" && !_openedLibFolderOnce)
        //     {
        //         ImGui.SetNextItemOpen(true);
        //         _openedLibFolderOnce = true;
        //     }
        //
        //     var isOpen = ImGui.TreeNode(subtree.Name);
        //     CustomComponents.ContextMenuForItem(() =>
        //                                         {
        //                                             if (ImGui.MenuItem("Rename Namespace"))
        //                                             {
        //                                                 _subtreeNodeToRename = subtree;
        //                                                 _renameNamespaceDialog.ShowNextFrame();
        //                                             }
        //                                         });
        //
        //     if (isOpen)
        //     {
        //         HandleDropTarget(subtree);
        //
        //         DrawNodeItems(subtree);
        //
        //         ImGui.TreePop();
        //     }
        //     else
        //     {
        //         if (DragAndDropHandling.IsDragging)
        //         {
        //             ImGui.SameLine();
        //             ImGui.PushID("DropButton");
        //             ImGui.Button("  <-", new Vector2(50, 15));
        //             HandleDropTarget(subtree);
        //             ImGui.PopID();
        //         }
        //     }
        //
        //     ImGui.PopID();
        // }
    }

    private void DrawNodeItems(NamespaceTreeNode subtree)
    {
        // Using a for loop to prevent modification during iteration exception
        for (var index = 0; index < subtree.Children.Count; index++)
        {
            var subspace = subtree.Children[index];
            DrawFolder(subspace);
        }

        for (var index = 0; index < subtree.Symbols.ToList().Count; index++)
        {
            var symbol = subtree.Symbols.ToList()[index];
            DrawAssetItem(symbol);
        }
    }

    // private static void HandleDropTarget(NamespaceTreeNode subtree)
    // {
    //     if (!DragAndDropHandling.TryGetDataDroppedLastItem(DragAndDropHandling.SymbolDraggingId, out var data))
    //         return;
    //
    //     if (!Guid.TryParse(data, out var symbolId))
    //         return;
    //
    //     if (!MoveSymbolToNamespace(symbolId, subtree.GetAsString(), out var reason))
    //         BlockingWindow.Instance.ShowMessageBox(reason, "Could not move symbol's namespace");
    // }

    // private static bool MoveSymbolToNamespace(Guid symbolId, string nameSpace, out string reason)
    // {
    //     if (!SymbolUiRegistry.TryGetSymbolUi(symbolId, out var symbolUi))
    //     {
    //         reason = $"Could not find symbol with id '{symbolId}'";
    //         return false;
    //     }
    //
    //     if (symbolUi.Symbol.Namespace == nameSpace)
    //     {
    //         reason = string.Empty;
    //         return true;
    //     }
    //
    //     if (symbolUi.Symbol.SymbolPackage.IsReadOnly)
    //     {
    //         reason = $"Could not move symbol [{symbolUi.Symbol.Name}] because its package is not modifiable";
    //         return false;
    //     }
    //
    //     return EditableSymbolProject.ChangeSymbolNamespace(symbolUi.Symbol, nameSpace, out reason);
    // }

    internal static void DrawAssetItem(Symbol symbol)
    {
        // ImGui.PushID(symbol.Id.GetHashCode());
        // {
        //     var color = symbol.OutputDefinitions.Count > 0
        //                     ? TypeUiRegistry.GetPropertiesForType(symbol.OutputDefinitions[0]?.ValueType).Color
        //                     : UiColors.Gray;
        //
        //     var symbolUi = symbol.GetSymbolUi();
        //
        //     // var state = ParameterWindow.GetButtonStatesForSymbolTags(symbolUi.Tags);
        //     // if (CustomComponents.IconButton(Icon.Bookmark, Vector2.Zero, state))
        //     // {
        //     //     
        //     // }
        //     if (ParameterWindow.DrawSymbolTagsButton(symbolUi))
        //         symbolUi.FlagAsModified();
        //
        //     ImGui.SameLine();
        //
        //     ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.OperatorBackground.Apply(color).Rgba);
        //     ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorVariations.OperatorBackgroundHover.Apply(color).Rgba);
        //     ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorVariations.OperatorBackgroundHover.Apply(color).Rgba);
        //     ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);
        //
        //     if (ImGui.Button(symbol.Name))
        //     {
        //         //_selectedSymbol = symbol;
        //     }
        //
        //     if (ImGui.IsItemHovered())
        //     {
        //         ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
        //
        //         if (!string.IsNullOrEmpty(symbolUi.Description))
        //         {
        //             ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 4));
        //             ImGui.BeginTooltip();
        //             ImGui.PushTextWrapPos(ImGui.GetFontSize() * 25.0f);
        //             ImGui.TextUnformatted(symbolUi.Description);
        //             ImGui.PopTextWrapPos();
        //             ImGui.PopStyleVar();
        //             ImGui.EndTooltip();
        //         }
        //     }
        //
        //     ImGui.PopStyleColor(4);
        //     //HandleDragAndDropForSymbolItem(symbol);
        //
        //     CustomComponents.ContextMenuForItem(drawMenuItems: () => CustomComponents.DrawSymbolCodeContextMenuItem(symbol),
        //                                         title: symbol.Name,
        //                                         id: "##symbolTreeSymbolContextMenu");
        //     //
        // }
        // ImGui.PopID();
    }

    // internal static void HandleDragAndDropForSymbolItem(Symbol symbol)
    // {
    //     if (IsSymbolCurrentCompositionOrAParent(symbol))
    //         return;
    //
    //     DragAndDropHandling.HandleDragSourceForLastItem(DragAndDropHandling.SymbolDraggingId, symbol.Id.ToString(), "Create instance");
    //
    //     if (!ImGui.IsItemDeactivated())
    //         return;
    //     
    //     var wasClick = ImGui.GetMouseDragDelta().Length() < 4;
    //     if (wasClick)
    //     {
    //         var components = ProjectView.Focused;
    //         if (components == null)
    //         {
    //             Log.Error($"No focused graph window found");
    //         }
    //         else if (components.NodeSelection.GetSelectedChildUis().Count() == 1)
    //         {
    //             ConnectionMaker.InsertSymbolInstance(components, symbol);
    //         }
    //     }
    // }
    //
    // private static bool IsSymbolCurrentCompositionOrAParent(Symbol symbol)
    // {
    //     var components = ProjectView.Focused;
    //     if (components?.CompositionInstance == null)
    //         return false;
    //
    //     var comp = components.CompositionInstance;
    //
    //     if (comp.Symbol.Id == symbol.Id)
    //     {
    //         return true;
    //     }
    //
    //     var instance = comp;
    //     while (instance != null)
    //     {
    //         if (instance.Symbol.Id == symbol.Id)
    //             return true;
    //
    //         instance = instance.Parent;
    //     }
    //
    //     return false;
    // }

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

                asset = new FileAsset
                            {
                                FileAliasPath = aliasedPath,
                                FileInfo = new FileInfo(absolutePath),
                                Package = package,
                            };
            }

            _allAssets.Add(asset);
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

    private readonly AssetTreeFolder _treeNode = new(AssetTreeFolder.RootNodeId);
    private readonly SymbolFilter _filter = new();

    private readonly List<FileAsset> _allAssets = [];
    private int _lastFileWatcherState = -1;

    private readonly Dictionary<string, FileAsset> _assetCache = [];
}

internal sealed class FileAsset
{
    public required string FileAliasPath;
    public IResourcePackage? Package;
    public required FileInfo FileInfo;
}