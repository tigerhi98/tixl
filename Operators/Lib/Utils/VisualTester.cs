using SharpDX;
using SharpDX.Direct3D11;

namespace Hidden
{
    class Class_VisualTester : TesterFunction
    {
        public override void SetupTestCase(OperatorPart subtree)
        {
            _offscreenRenderer.Setup(subtree, 200, 150);
        }

        public override bool GenerateData(OperatorPartContext context)
        {
            return _offscreenRenderer.RenderFrame(context);
        }

        public override void StoreAsReferenceData(int index, int count)
        {
            SetReferenceImage(index, count, _offscreenRenderer.ColorImage);
            RemoveFailureImages(index, count);
        }

        public override bool CompareData(int index, int count, float threshold, out string compareResultString)
        {
            compareResultString = String.Empty;
            bool result = false;
            Texture2D referenceImage = null;
            try
            {
                referenceImage = GetReferenceImage(index, count);
            }
            catch (SharpDXException ex)
            {
                compareResultString = String.Format("reference not loaded: {0}", ex);
                return false;
            }
            
            Texture2D diffColorImage = null;
            float deviation = CompareImage(_offscreenRenderer.ColorImage, referenceImage, ref diffColorImage);
            if (deviation < threshold)
            {
                RemoveFailureImages(index, count);
                result = true;
            }
            else
            {
                StoreImages(index, count, _offscreenRenderer.ColorImage, diffColorImage);
                result = false;
            }
            compareResultString = String.Format("{0:0.000}", deviation);

            Utilities.DisposeObj(ref referenceImage);
            Utilities.DisposeObj(ref diffColorImage);
            return result;
        }

        string GetReferenceFilename(int index, int count)
        {
            return GetReferenceFilename(index, count, ".dds");
        }

        Texture2D GetReferenceImage(int index, int count)
        {
            return Texture2D.FromFile<Texture2D>(D3DDevice.Device, GetReferenceFilename(index, count));
        }

        void SetReferenceImage(int index, int count, Texture2D image)
        {
            FileInfo fi = new FileInfo(GetReferenceFilename(index, count));
            if (!Directory.Exists(fi.DirectoryName))
                Directory.CreateDirectory(fi.DirectoryName);

            Texture2D.ToFile(D3DDevice.Device.ImmediateContext, image, ImageFileFormat.Dds, fi.FullName);
        }

        void StoreImages(int index, int count, Texture2D currentImage, Texture2D diffImage)
        {
            FileInfo fi = new FileInfo(GetReferenceFilename(index, count));
            if (!Directory.Exists(fi.DirectoryName))
                Directory.CreateDirectory(fi.DirectoryName);

            string path = Path.GetDirectoryName(fi.FullName);
            string filename = Path.GetFileNameWithoutExtension(fi.FullName);
            string ext = Path.GetExtension(fi.FullName);
            Texture2D.ToFile(D3DDevice.Device.ImmediateContext, currentImage, ImageFileFormat.Dds, String.Format("{0}/{1}.current{2}", path, filename, ext));
            Texture2D.ToFile(D3DDevice.Device.ImmediateContext, diffImage, ImageFileFormat.Dds, String.Format("{0}/{1}.diff{2}", path, filename, ext));
        }

        void RemoveFailureImages(int index, int count)
        {
            FileInfo fi = new FileInfo(GetReferenceFilename(index, count));
            string path = Path.GetDirectoryName(fi.FullName);
            string filename = Path.GetFileNameWithoutExtension(fi.FullName);
            string ext = Path.GetExtension(fi.FullName);
            string f1 = String.Format("{0}/{1}.current{2}", path, filename, ext);
            string f2 = String.Format("{0}/{1}.diff{2}", path, filename, ext);
            File.Delete(f1);
            File.Delete(f2);
        }

        float CompareImage(Texture2D current, Texture2D reference, ref Texture2D differenceImage)
        {
            if (current == null || reference == null ||
                current.Description.Width != reference.Description.Width ||
                current.Description.Height != reference.Description.Height ||
                current.Description.Format != reference.Description.Format)
            {
                return 1.0f;
            }

            var immediateContext = D3DDevice.Device.ImmediateContext;
            var currentDesc = new Texture2DDescription()
            {
                BindFlags = BindFlags.None,
                Format = current.Description.Format,
                Width = current.Description.Width,
                Height = current.Description.Height,
                MipLevels = current.Description.MipLevels,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                OptionFlags = ResourceOptionFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                ArraySize = 1
            };
            var currentWithCPUAccess = new Texture2D(D3DDevice.Device, currentDesc);
            immediateContext.CopyResource(current, currentWithCPUAccess);

            var referenceDesc = new Texture2DDescription()
            {
                BindFlags = BindFlags.None,
                Format = reference.Description.Format,
                Width = reference.Description.Width,
                Height = reference.Description.Height,
                MipLevels = reference.Description.MipLevels,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                OptionFlags = ResourceOptionFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                ArraySize = 1
            };
            var referenceWithCPUAccess = new Texture2D(D3DDevice.Device, referenceDesc);
            immediateContext.CopyResource(reference, referenceWithCPUAccess);

            var differenceDesc = new Texture2DDescription()
            {
                BindFlags = BindFlags.None,
                Format = current.Description.Format,
                Width = current.Description.Width,
                Height = current.Description.Height,
                MipLevels = current.Description.MipLevels,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                OptionFlags = ResourceOptionFlags.None,
                CpuAccessFlags = CpuAccessFlags.Write,
                ArraySize = 1
            };
            Utilities.DisposeObj(ref differenceImage);
            differenceImage = new Texture2D(D3DDevice.Device, differenceDesc);

            DataStream currentStream = null;
            DataBox currentDb = immediateContext.MapSubresource(currentWithCPUAccess, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out currentStream);
            currentStream.Position = 0;

            DataStream referenceStream = null;
            DataBox refDb = immediateContext.MapSubresource(referenceWithCPUAccess, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out referenceStream);
            referenceStream.Position = 0;

            DataStream differenceStream = null;
            DataBox diffDb = immediateContext.MapSubresource(differenceImage, 0, 0, MapMode.Write, SharpDX.Direct3D11.MapFlags.None, out differenceStream);
            differenceStream.Position = 0;

            double deviation = 0;
            for (int y = 0; y < current.Description.Height; ++y)
            {
                for (int x = 0; x < current.Description.Width; ++x)
                {
                    Color4 currentC = new Color4(currentStream.Read<Int32>());
                    Color4 referenceC = new Color4(referenceStream.Read<Int32>());
                    Color4 diffColor = currentC - referenceC;
                    Color4 absDiffColor = new Color4(Math.Abs(diffColor.Red), Math.Abs(diffColor.Green), Math.Abs(diffColor.Blue), 1.0f);
                    differenceStream.Write(absDiffColor.ToRgba());
                    deviation += Math.Abs(diffColor.Red) + Math.Abs(diffColor.Green) + Math.Abs(diffColor.Blue) + Math.Abs(diffColor.Alpha);
                }
                currentStream.Position += currentDb.RowPitch - current.Description.Width*4;
                referenceStream.Position += refDb.RowPitch - current.Description.Width*4;
                differenceStream.Position += diffDb.RowPitch - current.Description.Width*4;
            }
            deviation /= current.Description.Width*current.Description.Height;

            immediateContext.UnmapSubresource(currentWithCPUAccess, 0);
            Utilities.DisposeObj(ref currentStream);
            immediateContext.UnmapSubresource(referenceWithCPUAccess, 0);
            Utilities.DisposeObj(ref referenceStream);
            immediateContext.UnmapSubresource(differenceImage, 0);
            Utilities.DisposeObj(ref differenceStream);
            Utilities.DisposeObj(ref currentWithCPUAccess);
            Utilities.DisposeObj(ref referenceWithCPUAccess);
            return (float)deviation;
        }

        readonly OffScreenRenderer _offscreenRenderer = new OffScreenRenderer();
    }
}

