namespace Lib.test;

[Guid("ffce00b5-7883-4a80-81ae-e8229e531501")]
internal sealed class VisualTest : Instance<VisualTest>
{
    [Output(Guid = "b314c791-9c7d-442d-9ba1-36069cb4938d")]
    public readonly Slot<Command> Output = new();

    public VisualTest()
    {
        Output.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        var commands = Command.CollectedInputs;
        if (IsEnabled.GetValue(context))
        {
            // do preparation if needed
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i].Value?.PrepareAction?.Invoke(context);
            }

            // execute commands
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i].GetValue(context);
            }

            // cleanup after usage
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i].Value?.RestoreAction?.Invoke(context);
            }
        }

        Command.DirtyFlag.Clear();
    }

    [Input(Guid = "fab9f640-676b-4f27-a64f-0fcf4d7e9f1d")]
    public readonly MultiInputSlot<Command> Command = new();

    [Input(Guid = "aa858ec9-07ed-4070-b7ac-8186715cee60")]
    public readonly InputSlot<bool> IsEnabled = new();

}