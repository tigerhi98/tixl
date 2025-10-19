#nullable enable
using ImGuiNET;
using T3.Editor.Gui.Input;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Helpers;

namespace T3.Editor.Gui.Windows.SymbolLib;

/// <summary>
/// A simple ui to filter operators for certain properties like missing descriptions, invalid references, etc.
/// </summary>
/// <remarks>
/// It is only shown after scanning the library by pressing the update icon.
/// </remarks>
internal sealed class LibraryFiltering(SymbolLibrary symbolLibrary)
{
    internal void DrawSymbolFilters()
    {
        ImGui.SameLine();
        var status = _showFilters ? CustomComponents.ButtonStates.Activated : CustomComponents.ButtonStates.Dimmed;

        if (CustomComponents.IconButton(Icon.Flame, Vector2.Zero, status))
            _showFilters = !_showFilters;

        CustomComponents.TooltipForLastItem("Show problem filters", "Allows filter operators to different problems and attributes.");

        if (!_showFilters)
            return;

        ImGui.Indent();
        
        var opInfos = SymbolAnalysis.InformationForSymbolIds.Values;
        
        var totalOpCount = _onlyInLib
                               ? opInfos.Count(i => i.IsLibOperator)
                               : opInfos.Count;
        
        CustomComponents.SmallGroupHeader($"Out of {totalOpCount} show those with...");
        
        var needsUpdate = false;
        needsUpdate |= DrawFilterToggle("Help missing ({0})",
                                        opInfos.Count(i => i.LacksDescription && (i.IsLibOperator || !_onlyInLib)),
                                        Flags.MissingDescriptions,
                                        ref _activeFilters);
        
        needsUpdate |= DrawFilterToggle("Parameter help missing ({0})",
                                        opInfos.Count(i => i.LacksAllParameterDescription && (i.IsLibOperator || !_onlyInLib)),
                                        Flags.MissingAllParameterDescriptions,
                                        ref _activeFilters);
        
        needsUpdate |= DrawFilterToggle("Parameter help incomplete",
                                        0,
                                        Flags.MissingSomeParameterDescriptions,
                                        ref _activeFilters);
        
        needsUpdate |= DrawFilterToggle("No grouping ({0})",
                                        opInfos.Count(i => i.LacksParameterGrouping && (i.IsLibOperator || !_onlyInLib)),
                                        Flags.MissingParameterGrouping,
                                        ref _activeFilters);
        
        needsUpdate |= DrawFilterToggle("Unused ({0})",
                                        opInfos.Count(i => i.DependingSymbols.Count == 0 && (i.IsLibOperator || !_onlyInLib)),
                                        Flags.Unused,
                                        ref _activeFilters);
        
        needsUpdate |= DrawFilterToggle("Invalid Op dependencies ({0})",
                                        opInfos.Count(i => i.InvalidRequiredIds.Count > 0 && (i.IsLibOperator || !_onlyInLib)),
                                        Flags.InvalidRequiredOps,
                                        ref _activeFilters);

        needsUpdate |= DrawFilterToggle("Depends on obsolete ops ({0})",
                                        opInfos.Count(i => i.DependsOnObsoleteOps && (i.IsLibOperator || !_onlyInLib)),
                                        Flags.DependsOnObsoleteOps,
                                        ref _activeFilters);
        
        FormInputs.AddVerticalSpace(5);

        needsUpdate |= DrawFilterToggle("Obsolete ({0})",
                                        opInfos.Count(i => i.Tags.HasFlag(SymbolUi.SymbolTags.Obsolete) && (i.IsLibOperator || !_onlyInLib)),
                                        Flags.Obsolete,
                                        ref _activeFilters);

        needsUpdate |= DrawFilterToggle("NeedsFix ({0})",
                                        opInfos.Count(i => i.Tags.HasFlag(SymbolUi.SymbolTags.NeedsFix) && (i.IsLibOperator || !_onlyInLib)),
                                        Flags.NeedsFix,
                                        ref _activeFilters);

        
        
        
        FormInputs.AddVerticalSpace(5);
        needsUpdate |= ImGui.Checkbox("Only in Lib", ref _onlyInLib);
        
        ImGui.Unindent();


        if (needsUpdate)
        {
            symbolLibrary.FilteredTree.PopulateCompleteTree(s =>
                                                            {
                                                                var info = SymbolAnalysis.InformationForSymbolIds[s.Symbol.Id];
                                                                if (_onlyInLib && !info.IsLibOperator)
                                                                    return false;

                                                                if (!AnyFilterActive)
                                                                    return true;

                                                                return _activeFilters.HasFlag(Flags.MissingDescriptions) && info.LacksDescription
                                                                       || _activeFilters.HasFlag(Flags.MissingAllParameterDescriptions) && info.LacksAllParameterDescription
                                                                       || _activeFilters.HasFlag(Flags.MissingSomeParameterDescriptions) && info.LacksSomeParameterDescription
                                                                       || _activeFilters.HasFlag(Flags.MissingParameterGrouping) && info.LacksParameterGrouping
                                                                       || _activeFilters.HasFlag(Flags.InvalidRequiredOps) && info.InvalidRequiredIds.Count > 0
                                                                       || _activeFilters.HasFlag(Flags.Unused) && info.DependingSymbols.Count == 0
                                                                       || _activeFilters.HasFlag(Flags.Obsolete) && info.Tags.HasFlag(SymbolUi.SymbolTags.Obsolete)
                                                                       || _activeFilters.HasFlag(Flags.NeedsFix) && info.Tags.HasFlag(SymbolUi.SymbolTags.NeedsFix)
                                                                       || _activeFilters.HasFlag(Flags.DependsOnObsoleteOps) && info.DependsOnObsoleteOps
                                                                    ;
                                                            });
        }

        ImGui.Separator();
        FormInputs.AddVerticalSpace();
        CustomComponents.SmallGroupHeader($"Result...");
    }

    private static bool DrawFilterToggle(string label, int count, Flags filterFlag, ref Flags activeFlags)
    {
        var isActive = activeFlags.HasFlag(filterFlag);
        var clicked = ImGui.Checkbox(string.Format(label, count), ref isActive);
        if (clicked)
        {
            activeFlags ^= filterFlag;
        }

        return clicked;
    }

    [Flags]
    private enum Flags
    {
        None = 0, // auto increment shift
        MissingDescriptions = 1<<1,
        MissingAllParameterDescriptions = 1 << 2,
        MissingSomeParameterDescriptions = 1 << 3,
        MissingParameterGrouping = 1 << 4,
        InvalidRequiredOps = 1 << 5,
        Unused = 1 << 6,
        Obsolete = 1<<7,
        NeedsFix = 1<<8,
        DependsOnObsoleteOps = 1<<9,

    }

    private bool _onlyInLib = true;
    private Flags _activeFilters;
    
    private bool _showFilters;
    internal bool AnyFilterActive => _activeFilters != Flags.None;
}