﻿#nullable enable
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.UiModel.Helpers;

namespace T3.Editor.Gui.Windows.AssetLib;

/// <summary>
/// Holds the complete ui state of an asset library window.
/// This is then passed on the subcomponents for rendering content.
/// </summary>
internal sealed class AssetLibState
{
    
    /// <summary>
    /// All assets found in resource folders.
    /// This is completely cleared and recreated on external file changes.  
    /// </summary>
    public readonly List<AssetItem> AllAssets = [];
    
    /// <summary>
    /// Stores assetItem data by alias path
    /// </summary>
    public readonly Dictionary<string, AssetItem> AssetCache = [];
    
    /// <summary>
    /// The available / relevant resource folders depends on the context of the current composition instance.
    /// When I'm in a lib-operator, we don't want to show (or expose) files outside of this context.
    /// </summary>
    public Instance? Composition;
    
    /// <summary>
    /// If a child is selected and shown in the parameter window, we can indicate which items it's supports. 
    /// </summary>
    public Instance? ActiveInstance;
    
    /// <summary>
    /// If this is not null, the ActiveInstance has a string-input with FilePath usage.
    /// We can access or set this input to update its referenced resource.
    /// </summary>
    public InputSlot<string>? ActivePathInput;
    
    /// <summary>
    /// We need to indicate if a closed folder contains the file referenced in the <see cref="ActivePathInput"/> 
    /// </summary>
    public string? ActiveAbsolutePath;
    
    /// <summary>
    /// List of extensions than can be opened by selected operator
    /// </summary>
    internal static List<int> CompatibleExtensionIds = [];
    
    public readonly AssetFolder RootFolder = new(AssetFolder.RootNodeId, null);
    
    public readonly SymbolFilter Filter = new();

    #region internal
    /// <summary>
    /// An internal counter to check if any of the resource folders have changed externally.
    /// If changed we completely rescan ResourceFolders.
    /// </summary>
    public int LastFileWatcherState = -1;
    
    public bool OpenedLibFolderOnce;
    #endregion
    
}