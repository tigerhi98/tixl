using ManagedBass;
using T3.Core.Audio;
using T3.Core.Utils;

namespace Lib.io.audio._;

[Guid("55E338F7-6D8C-4B61-8E10-9BB4D5FCFF91")]
internal sealed class AudioWaveform : Instance<AudioFrequencies>
{
    [Output(Guid = "AB0976EB-2B14-4AA2-A12C-398310A6E07B", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
    public readonly Slot<List<float>> Left = new();

    [Output(Guid = "71310214-FD15-49EE-9C25-6200AA1E8566", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
    public readonly Slot<List<float>> Right = new();

    public AudioWaveform()
    {
        Left.UpdateAction = UpdateLeft;
        Right.UpdateAction = UpdateRight;
    }

    private void UpdateLeft(EvaluationContext context)
    {
        Left.Value  =  AudioAnalysis.WaveformLeftBuffer.ToList();
    }
    private void UpdateRight(EvaluationContext context)
    {
        Right.Value = AudioAnalysis.WaveformRightBuffer.ToList();
    }
}