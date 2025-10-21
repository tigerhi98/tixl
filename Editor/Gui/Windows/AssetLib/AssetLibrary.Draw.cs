using ImGuiNET;
using T3.Core.Operator;
using T3.Core.SystemUi;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows.AssetLib;

internal sealed partial class AssetLibrary
{
    protected override void DrawContent()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 10);

        DrawLibContent();

        ImGui.PopStyleVar(1);
    }

    private void DrawLibContent()
    {
        UpdateAssetsIfRequired();

        var iconCount = 1;

        CustomComponents.DrawInputFieldWithPlaceholder("Search symbols...", ref _filter.SearchString, -ImGui.GetFrameHeight() * iconCount + 16);

        ImGui.BeginChild("scrolling", Vector2.Zero, false, ImGuiWindowFlags.NoBackground);
        {
            DrawFolder(_rootNode);
        }
        ImGui.EndChild();
    }

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

            var isOpen = ImGui.TreeNode(folder.Name);
            CustomComponents.ContextMenuForItem(() =>
                                                {
                                                    if (ImGui.MenuItem("Open in Explorer"))
                                                    {
                                                        if (folder.TryGetAbsolutePath(out var absolutePath))
                                                        {
                                                            CoreUi.Instance.OpenWithDefaultApplication(absolutePath);
                                                        }
                                                        else
                                                        {
                                                            Log.Warning($"Failed to get path for {folder.GetAliasPath()}");
                                                        }
                                                    }
                                                });

            if (isOpen)
            {
                //HandleDropTarget(subtree);

                DrawFolderContent(folder);

                ImGui.TreePop();
            }
            // else
            // {
            //     if (DragAndDropHandling.IsDragging)
            //     {
            //         ImGui.SameLine();
            //         ImGui.PushID("DropButton");
            //         ImGui.Button("  <-", new Vector2(50, 15));
            //         HandleDropTarget(subtree);
            //         ImGui.PopID();
            //     }
            // }

            ImGui.PopID();
        }
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

    private static void DrawAssetItem(AssetItem asset)
    {
        ImGui.TextUnformatted(asset.FileInfo.Name);

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
}