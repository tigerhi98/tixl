#nullable enable

using System.Diagnostics.CodeAnalysis;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows.AssetLib;

public static class AssetTypeRegistry
{
    public sealed class AssetType
    {
        public string Name;
        public List<int> ExtensionIds;
        public required List<Guid> PrimaryOperators;
        public required Color Color;
        public required Icon Icon;

        internal AssetType(string name, List<int> extensionIds)
        {
            Name = name;
            ExtensionIds = extensionIds;
            foreach (var id in extensionIds)
            {
                _assetTypeForId[id] = this;
            }
        }
    }

    internal static bool TryGetFromId(int id, [NotNullWhen(true)] out AssetType? type)
    {
        return _assetTypeForId.TryGetValue(id, out type);
    }

    private static List<AssetType> InitTypes()
    {
        return
            [
                new AssetType("Obj", [FileExtensionRegistry.GetId("obj")])
                    {
                        PrimaryOperators = [new Guid("be52b670-9749-4c0d-89f0-d8b101395227")], // LoadObj
                        Color = UiColors.ColorForGpuData,
                        Icon = Icon.FileGeometry,
                    },

                new AssetType("Image", [
                        FileExtensionRegistry.GetId("png"),
                        FileExtensionRegistry.GetId("jpg"),
                        FileExtensionRegistry.GetId("jpeg"),
                        FileExtensionRegistry.GetId("bmp"),
                        FileExtensionRegistry.GetId("tga"),
                        FileExtensionRegistry.GetId("gif"),
                        FileExtensionRegistry.GetId("dds"),
                    ])
                    {
                        PrimaryOperators = [new Guid("0b3436db-e283-436e-ba85-2f3a1de76a9d")], // Load Image
                        Color = UiColors.ColorForTextures,
                        Icon = Icon.FileImage,
                    },

                new AssetType("Shader", [FileExtensionRegistry.GetId("hlsl")])
                    {
                        PrimaryOperators =
                            [
                                new Guid("a256d70f-adb3-481d-a926-caf35bd3e64c"), // ComputeShader
                                // TODO: add more
                            ],
                        Color = UiColors.ColorForGpuData,
                        Icon = Icon.FileShader,
                    },

                new AssetType("Gltf", [
                        FileExtensionRegistry.GetId("glb"),
                        FileExtensionRegistry.GetId("gltf"),
                    ])
                    {
                        PrimaryOperators =
                            [
                                new Guid("92b18d2b-1022-488f-ab8e-a4dcca346a23"), // LoadGltf
                                new Guid("00618c91-f39a-44ea-b9d8-175c996460dc"), // LoadGltfScene
                                // TODO: add more
                            ],
                        Color = UiColors.ColorForGpuData,
                        Icon = Icon.FileGeometry,
                    },
            ];
    }

    private static readonly Dictionary<int, AssetType> _assetTypeForId = [];
    private static List<AssetType> _assetTypes = InitTypes();
}