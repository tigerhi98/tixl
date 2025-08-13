#nullable enable
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.OpUis.WidgetUi;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.OpUis.UIs;

internal static class SetVec3Ui
{
    private sealed class Binding : OpUiBinding
    {
        internal Binding(Instance instance)
        {
            IsValid = AutoBind(instance);
        }
        
        [BindInput("E1034127-63C9-42ED-9BDD-D1BC054BD103")]
        internal readonly InputSlot<Vector3> Value = null!;

        [BindInput("0edf7837-4555-4e62-902f-930abf72e8b8")]
        internal readonly InputSlot<string> VariableName = null!;

    }

    public static OpUi.CustomUiResult DrawChildUi(Instance instance,
                                                  ImDrawListPtr drawList,
                                                  ImRect area,
                                                  Vector2 canvasScale,
                                                  ref OpUiBinding? data1)
    {
        data1 ??= new Binding(instance);
        var data = (Binding)data1;

        if (!data.IsValid)
            return OpUi.CustomUiResult.PreventOpenSubGraph;


        var symbolChild = instance.SymbolChild;
        drawList.PushClipRect(area.Min, area.Max, true);

        var value = data.Value.TypedInputValue.Value;

        if (!string.IsNullOrWhiteSpace(symbolChild.Name))
        {
            WidgetElements.DrawPrimaryTitle(drawList, area, symbolChild.Name, canvasScale);
        }
        else
        {
            WidgetElements.DrawPrimaryTitle(drawList, area, "Vec3 " + data.VariableName.TypedInputValue.Value + " =", canvasScale);
        }

        WidgetElements.DrawSmallValue(drawList, area, $"{value}", canvasScale);

        drawList.PopClipRect();
        return OpUi.CustomUiResult.Rendered | OpUi.CustomUiResult.PreventInputLabels | OpUi.CustomUiResult.PreventOpenSubGraph;
    }
}