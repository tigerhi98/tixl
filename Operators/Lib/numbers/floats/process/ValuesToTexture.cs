#nullable enable
using SharpDX;
using T3.Core.Utils;
using Utilities = T3.Core.Utils.Utilities;

namespace Lib.numbers.floats.process;

[Guid("55cc0f79-96c9-482e-9794-934dc0f87708")]
internal sealed class ValuesToTexture : Instance<ValuesToTexture>
{
    [Output(Guid = "f01099a0-a196-4689-9900-edac07908714")]
    public readonly Slot<Texture2D> CurveTexture = new();

    public ValuesToTexture()
    {
        CurveTexture.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        if (!Values.HasInputConnections)
            return;

        var useHorizontal = Direction.GetValue(context) == 0;
        var values = Values.GetValue(context);
        if (values == null || values.Count == 0)
            return;

        var rangeStart = RangeStart.GetValue(context).Clamp(0, values.Count - 1);
        var rangeEnd = RangeEnd.GetValue(context).Clamp(0, values.Count - 1);

        if (UseFullList.GetValue(context))
        {
            rangeStart = 0;
            rangeEnd = values.Count - 1;
        }

        var gain = Gain.GetValue(context);
        var pow = Pow.GetValue(context);

        if (Math.Abs(pow) < 0.001f)
            return;

        if (rangeEnd < rangeStart)
            (rangeEnd, rangeStart) = (rangeStart, rangeEnd);

        var sampleCount = (rangeEnd - rangeStart) + 1;
        const int entrySizeInBytes = sizeof(float);
        var rowPitch = useHorizontal ? sampleCount * entrySizeInBytes : entrySizeInBytes;

        // Ensure buffer large enough
        if (_floatBuffer.Length < sampleCount)
        {
            if (_floatBufferHandle.IsAllocated)
                _floatBufferHandle.Free();

            _floatBuffer = new float[sampleCount];
            _floatBufferHandle = GCHandle.Alloc(_floatBuffer, GCHandleType.Pinned);
            _floatBufferPtr = _floatBufferHandle.AddrOfPinnedObject();
        }

        // Fill buffer
        for (var i = 0; i < sampleCount; i++)
        {
            var v = (float)Math.Pow(values[rangeStart + i] * gain, pow);
            _floatBuffer[i] = v;
        }

        // Row pitch is bytes per row (stride)
        var height = useHorizontal ? 1 : sampleCount;

        // Create texture if needed (no initial data)
        if (CurveTexture.Value == null ||
            CurveTexture.Value.Description.Width != (useHorizontal ? sampleCount : 1) ||
            CurveTexture.Value.Description.Height != (useHorizontal ? 1 : sampleCount))
        {
            if (CurveTexture.Value != null)
                Utilities.Dispose(ref CurveTexture.Value);

            var texture2DDescription = new Texture2DDescription
                                           {
                                               Width = useHorizontal ? sampleCount : 1,
                                               Height = useHorizontal ? 1 : sampleCount,
                                               ArraySize = 1,
                                               BindFlags = BindFlags.ShaderResource,
                                               Usage = ResourceUsage.Default,
                                               MipLevels = 1,
                                               CpuAccessFlags = CpuAccessFlags.None,
                                               Format = Format.R32_Float,
                                               SampleDescription = new SampleDescription(1, 0),
                                           };
            CurveTexture.Value = Texture2D.CreateTexture2D(texture2DDescription);
        }

        // Upload with DataBox (not DataRectangle)
        var dataBox = new DataBox(_floatBufferPtr, rowPitch, rowPitch * height);
        ResourceManager.Device.ImmediateContext.UpdateSubresource(dataBox, CurveTexture.Value, 0);
    }

    // Reused, pinned upload buffer (avoid per-frame allocations)
    private GCHandle _floatBufferHandle;
    private IntPtr _floatBufferPtr = IntPtr.Zero;

    private float[] _floatBuffer = [];

    [Input(Guid = "092C8D1F-A70E-4298-B5DF-52C9D62F8E04")]
    public readonly InputSlot<List<float>> Values = new();

    [Input(Guid = "165F7E0E-6EF0-4BE1-8ED3-61ED0DB752ED")]
    public readonly InputSlot<bool> UseFullList = new();

    [Input(Guid = "CA67BFAF-EDE7-43BA-B279-FC1DDFBE2FFA")]
    public readonly InputSlot<int> RangeStart = new();

    [Input(Guid = "DB868176-D51C-41AA-BAFE-68C3E50E725E")]
    public readonly InputSlot<int> RangeEnd = new();

    [Input(Guid = "CC812393-F080-4E17-A525-15B09F8ACDD0")]
    public readonly InputSlot<float> Gain = new();

    [Input(Guid = "51545316-69FC-441F-B59F-44979E32972C")]
    public readonly InputSlot<float> Pow = new();

    [Input(Guid = "63E90D86-5AD5-4333-8B99-7F8D285C4913", MappedType = typeof(Directions))]
    public readonly InputSlot<int> Direction = new();

    private enum Directions
    {
        Horizontal,
        Vertical,
    }
}