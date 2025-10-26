#nullable enable
using System.Runtime.CompilerServices;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator.Slots;
using T3.Core.SystemUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel.Commands;
using T3.Editor.UiModel.Commands.Graph;

namespace T3.Editor.Gui.Windows.AssetLib;

internal sealed partial class AssetLibrary
{
    private void DrawLibContent()
    {
        var iconCount = 1;

        CustomComponents.DrawInputFieldWithPlaceholder("Search symbols...",
                                                       ref _filter.SearchString,
                                                       -ImGui.GetFrameHeight() * iconCount + 16);

        ImGui.BeginChild("scrolling", Vector2.Zero, false, ImGuiWindowFlags.NoBackground);
        {
            DrawFolder(_rootNode);
        }
        ImGui.EndChild();
    }

    private bool _expandToFileTriggered;

    private void DrawFolder(AssetFolder folder)
    {
        if (folder.Name == AssetFolder.RootNodeId)
        {
            DrawFolderContent(folder);
        }
        else
        {
            ImGui.PushID(folder.Name);
            ImGui.SetNextItemWidth(10);
            if (folder.Name == "Lib" && !_openedLibFolderOnce)
            {
                ImGui.SetNextItemOpen(true);
                _openedLibFolderOnce = true;
            }

            if (_expandToFileTriggered && ContainsTargetFile(folder))
            {
                ImGui.SetNextItemOpen(true);
            }

            var isOpen = ImGui.TreeNode(folder.Name);
            CustomComponents.ContextMenuForItem(() =>
                                                {
                                                    if (ImGui.MenuItem("Open in Explorer"))
                                                    {
                                                        if (!string.IsNullOrEmpty(folder.AbsolutePath))
                                                        {
                                                            CoreUi.Instance.OpenWithDefaultApplication(folder.AbsolutePath);
                                                        }
                                                        else
                                                        {
                                                            Log.Warning($"Failed to get path for {folder.AliasPath}");
                                                        }
                                                    }
                                                });

            if (isOpen)
            {
                HandleDropTarget(folder);

                DrawFolderContent(folder);

                ImGui.TreePop();
            }
            else
            {
                var containsTargetFile = ContainsTargetFile(folder);

                if (containsTargetFile)
                {
                    var h = ImGui.GetFontSize();
                    var x = ImGui.GetContentRegionAvail().X - h;

                    ImGui.SameLine(x); // N pixels gap after the node
                    if (CustomComponents.IconButton(Icon.Knob, new Vector2(h)))
                    {
                        _expandToFileTriggered = true;
                    }
                }

                if (DragAndDropHandling.IsDraggingWith(DragAndDropHandling.AssetDraggingId))
                {
                    ImGui.SameLine();
                    ImGui.PushID("DropButton");
                    ImGui.Button("  <-", new Vector2(50, 15));
                    //HandleDropTarget(subtree);
                    ImGui.PopID();
                }
            }

            ImGui.PopID();
        }
    }

    private bool ContainsTargetFile(AssetFolder folder)
    {
        var containsTargetFile = _activePathInput != null
                                 && !string.IsNullOrEmpty(folder.AbsolutePath)
                                 && !string.IsNullOrEmpty(_activeAbsolutePath) 
                                 && _activeAbsolutePath.StartsWith(folder.AbsolutePath);
        return containsTargetFile;
    }

    private void DrawFolderContent(AssetFolder folder)
    {
        // Using a for loop to prevent modification during iteration exception
        for (var index = 0; index < folder.SubFolders.Count; index++)
        {
            var subspace = folder.SubFolders[index];
            DrawFolder(subspace);
        }

        for (var index = 0; index < folder.FolderAssets.ToList().Count; index++)
        {
            DrawAssetItem(folder.FolderAssets.ToList()[index]);
        }
    }

    private void DrawAssetItem(AssetItem asset)
    {
        ImGui.PushID(RuntimeHelpers.GetHashCode(asset));
        {
            var defaultId = string.Empty;
            var isSelected = asset.AbsolutePath == _activeAbsolutePath;

            var fade = CompatibleExtensionIds.Count == 0
                           ? 0.7f 
                            : !CompatibleExtensionIds.Contains(asset.FileExtensionId) ? 0.2f : 1f;


            var defaultColor = asset.AssetType?.Color ?? UiColors.Text;
            //var iconColor = isSelected ? UiColors.StatusActivated : defaultColor.Fade(fade);
            var icon = asset.AssetType?.Icon ?? Icon.FileImage;
            
            if (ButtonWithIcon(defaultId, 
                               asset.FileInfo.Name, 
                               icon, 
                               defaultColor.Fade(fade),
                               UiColors.Text.Fade(fade),
                               isSelected
                               ))
            {
                var stringInput = _activePathInput;
                if (stringInput != null && !isSelected)
                {
                    _activeAbsolutePath = asset.AbsolutePath;

                    ApplyResourcePath(asset, stringInput);
                }
            }

            // Stop expanding if item becomes visible
            if (isSelected && _expandToFileTriggered)
            {
                _expandToFileTriggered = false;
                ImGui.SetScrollHereY(1f);
            }

            CustomComponents.ContextMenuForItem(drawMenuItems: () =>
                                                               {
                                                                   if (ImGui.MenuItem("Edit externally"))
                                                                   {
                                                                       CoreUi.Instance.OpenWithDefaultApplication(asset.FileInfo.FullName);
                                                                       Log.Debug("Not implemented yet");
                                                                   }
                                                               },
                                                title: asset.FileInfo.Name,
                                                id: "##symbolTreeSymbolContextMenu");

            DragAndDropHandling.HandleDragSourceForLastItem(DragAndDropHandling.SymbolDraggingId, asset.FileAliasPath, "Move or use asset");

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll); // Indicator for drag

                // Tooltip
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 4));
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 25.0f);
                    ImGui.TextUnformatted($"""
                                           Filesize: {asset.FileInfo.Length}
                                           Path: {asset.FileInfo.Directory}
                                           Time: {asset.FileInfo.LastWriteTime}
                                           """);
                    ImGui.PopTextWrapPos();
                    ImGui.PopStyleVar();
                    ImGui.EndTooltip();
                }
            }

            // Click
            if (!ImGui.IsItemDeactivated())
                return;

            var wasClick = ImGui.GetMouseDragDelta().Length() < 4;
            if (wasClick)
            {
                // TODO: implement
            }
        }

        ImGui.PopID();
    }

    // TODO: Clean up and move to custom components
    private static bool ButtonWithIcon(string id, string label, Icon icon, Color iconColor, Color textColor, bool selected)
    {
        var cursorPos = ImGui.GetCursorScreenPos();
        var frameHeight = ImGui.GetFrameHeight();

        var dummyDim = new Vector2(frameHeight);
        if (!ImGui.IsRectVisible(cursorPos, cursorPos + dummyDim))
        {
            ImGui.Dummy(dummyDim); // maintain layout spacing
            return false;
        }

        var iconSize = Icons.FontSize;
        var padding = 4f;
        Vector2 iconDim = new(iconSize);

        var textSize = ImGui.CalcTextSize(label);
        var buttonSize = new Vector2(iconDim.X + padding + textSize.X + padding * 2,
                                     Math.Max(iconDim.Y + padding * 2, ImGui.GetFrameHeight()));

        var pressed = ImGui.InvisibleButton(id, buttonSize);
        
        var drawList = ImGui.GetWindowDrawList();
        var buttonMin = ImGui.GetItemRectMin();
        var buttonMax = ImGui.GetItemRectMax();
        if (selected)
        {
            drawList.AddRect(buttonMin, buttonMax, UiColors.StatusActivated, 5);
        }
        

        var iconPos = new Vector2(buttonMin.X + padding,
                                  (int)(buttonMin.Y + (buttonSize.Y - iconDim.Y) * 0.5f) + 1);

        Icons.GetGlyphDefinition(icon, out var uvRange, out _);
        drawList.AddImage(ImGui.GetIO().Fonts.TexID,
                               iconPos,
                               iconPos + iconDim,
                               uvRange.Min,
                               uvRange.Max,
                               iconColor.Fade(0.5f));

        Vector2 textPos = new(iconPos.X + iconDim.X + padding,
                              buttonMin.Y + (buttonSize.Y - textSize.Y) * 0.5f);

        drawList.AddText(textPos, textColor, label);
        return pressed;
    }

    private static void ApplyResourcePath(AssetItem asset, InputSlot<string> inputSlot)
    {
        var instance = inputSlot.Parent;
        var composition = instance.Parent;
        if (composition == null)
        {
            Log.Warning("Can't find composition to apply resource path");
            return;
        }

        inputSlot.Input.IsDefault = false;

        var changeInputValueCommand = new ChangeInputValueCommand(composition.Symbol,
                                                                  instance.SymbolChildId,
                                                                  inputSlot.Input,
                                                                  inputSlot.Input.Value);
        
        // warning: we must not use Value because this will use by abstract resource to detect changes
        inputSlot.TypedInputValue.Value = asset.FileAliasPath;

        inputSlot.DirtyFlag.ForceInvalidate();
        inputSlot.Parent.Parent?.Symbol.InvalidateInputInAllChildInstances(inputSlot);
        changeInputValueCommand.AssignNewValue(inputSlot.Input.Value);
        UndoRedoStack.Add(changeInputValueCommand);
    }

    private static void HandleDropTarget(AssetFolder subtree)
    {
        if (!DragAndDropHandling.TryGetDataDroppedLastItem(DragAndDropHandling.AssetDraggingId, out var data))
            return;

        // TODO: Implement dragging of files

        // if (!Guid.TryParse(data, out var path))
        //     return;
        //
        // if (!MoveSymbolToNamespace(path, subtree.GetAsString(), out var reason))
        //     BlockingWindow.Instance.ShowMessageBox(reason, "Could not move symbol's namespace");
    }
}