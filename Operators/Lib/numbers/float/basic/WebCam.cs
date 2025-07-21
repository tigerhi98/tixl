namespace Lib.numbers.@float.basic;

using SharpDX;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using MediaFoundation;
using MediaFoundation.Misc;
using MediaFoundation.ReadWrite;

[Guid("ee36ca64-0466-4aa0-991b-c09b9978b615")]
internal sealed class WebCam : Instance<WebCam>, ICustomDropdownHolder
{
    [Output(Guid = "8CF6F0BB-5790-4EA4-A329-776E60D688B6", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
    public readonly Slot<T3.Core.DataTypes.Texture2D> Texture = new();

    public WebCam()
    {
        Texture.UpdateAction = Update;
    }

    private void Update(EvaluationContext context)
    {
        if (_sourceReader == null)
            TryInitializeCapture();

        if (_sourceReader != null)
            TryReadFrame();
    }

    private void TryInitializeCapture()
    {
        _webcams = WebcamHelper.EnumerateWebcams();

        var deviceName = InputDeviceName.Value;
        if (!TryFindDevice(deviceName, out var imfActivate))
            return;

        imfActivate.ActivateObject(typeof(IMFMediaSource).GUID, out var mediaSourceObj);
        var mediaSource = (IMFMediaSource)mediaSourceObj;

        IMFAttributes attr;
        MFExtern.MFCreateAttributes(out attr, 1);
        attr.SetUINT64(MFAttributesClsid.MF_SOURCE_READER_D3D_MANAGER, _dxgiManager.NativePointer.ToInt64());
        

        IMFSourceReader sourceReader;
        MFExtern.MFCreateSourceReaderFromMediaSource(mediaSource, attr, out sourceReader);

        //_sourceReader = new SourceReader(sourceReader);
        MFExtern.MFCreateSourceReaderFromMediaSource(mediaSource, attr, out _sourceReader);

        IMFMediaType mediaType;
        MFExtern.MFCreateMediaType(out mediaType);
        mediaType.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video);
        mediaType.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.RGB32);

        var reserved = new MFInt(0);
        _sourceReader.SetCurrentMediaType(0, reserved, mediaType);
        //_sourceReader.SetCurrentMediaType(SourceReaderIndex.FirstVideoStream, IntPtr.Zero, mediaType);
        //_sourceReader.SetCurrentMediaType(0, IntPtr.Zero, mediaType);

        mediaSource.CreatePresentationDescriptor(out var presentationDescriptor);
        mediaSource.Start(presentationDescriptor, Guid.Empty, null);
    }

    private void TryReadFrame()
    {
        IMFSample sample;
        MF_SOURCE_READER_FLAG streamFlags;
        long timestamp;
        int actualStreamIndex;

        _sourceReader.ReadSample(
                                 0, // First video stream
                                 MF_SOURCE_READER_CONTROL_FLAG.None,
                                 out actualStreamIndex,
                                 out streamFlags,
                                 out timestamp,
                                 out sample
                                );

        if (sample == null)
            return;

        IMFMediaBuffer buffer;
        sample.ConvertToContiguousBuffer(out buffer);
        
        buffer.Lock(out var ptr, out var maxLen, out var currentLen);

        if (_gpuTexture == null || _bufferSize != currentLen)
            CreateOrResizeTexture(currentLen);

        ResourceManager.Device.ImmediateContext.UpdateSubresource(
            new DataBox(ptr, 0, 0),
            _gpuTexture,
            0);

        buffer.Unlock();
        Marshal.ReleaseComObject(buffer);
        Marshal.ReleaseComObject(sample);

        if(_gpuTexture != null)
            Texture.Value = new T3.Core.DataTypes.Texture2D( _gpuTexture);
    }

    private void CreateOrResizeTexture(int bufferSize)
    {
        int width = 640;
        int height = 480;

        _gpuTexture?.Dispose();
        _gpuTexture = new Texture2D(ResourceManager.Device, new Texture2DDescription
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
            SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        });

        _bufferSize = bufferSize;
    }

    private bool TryFindDevice(string name, out IMFActivate activate)
    {
        if (string.IsNullOrEmpty(name))
        {
            activate = null;
            return false;
        }
        
        activate = null;
        return _webcams.TryGetValue(name, out activate);
    }

    IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
    {
        foreach (var deviceName in _webcams.Keys)
            yield return deviceName;
    }

    string ICustomDropdownHolder.GetValueForInput(Guid inputId) => InputDeviceName.Value;

    void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
    {
        InputDeviceName.SetTypedInputValue(result);
    }

    protected override void Dispose(bool disposing)
    {
        _gpuTexture?.Dispose();

        if (_sourceReader != null)
        {
            Marshal.ReleaseComObject(_sourceReader);
            _sourceReader = null;
        }        
    }

    [Input(Guid = "c90e2095-16a7-4428-83f4-05a90b0b92d2")]
    public readonly InputSlot<string> InputDeviceName = new();

    

    public static class WebcamHelper
    {
        public static Dictionary<string, IMFActivate> EnumerateWebcams()
        {
            var results = new Dictionary<string, IMFActivate>();

            MFExtern.MFStartup(0x00020070, MFStartup.Full);

            IMFAttributes attributes;
            MFExtern.MFCreateAttributes(out attributes, 1);
            
            attributes.GetCount(out int attrCount);
            
            attributes.SetGUID(MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_CATEGORY);

            
            IMFActivate[] devices;
            int count;
            MFExtern.MFEnumDeviceSources(attributes, out devices, out count);

            for (int i = 0; i < count; i++)
            {
                devices[i].GetAllocatedString(MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME, out var name, out _);
                results[name] = devices[i];
            }

            Marshal.ReleaseComObject(attributes);

            MFExtern.MFShutdown();

            return results;
        }
    }

    private Dictionary<string, IMFActivate> _webcams = new();
    private Texture2D _gpuTexture;
    private int _bufferSize;
    private IMFSourceReader _sourceReader;
    private readonly SharpDX.MediaFoundation.DXGIDeviceManager _dxgiManager = new();
}
