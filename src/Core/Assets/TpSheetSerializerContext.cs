using System.Text.Json.Serialization;

namespace MegaCrit.Sts2.Core.Assets;

/// <summary>
/// Source-generated JSON contract for TexturePacker .tpsheet files.
/// Required under NativeAOT: reflection-based serialization is disabled there
/// (System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault = false), so every
/// serializer call must resolve its contract through a JsonTypeInfo.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TpSheetData))]
[JsonSerializable(typeof(TpSheetTexture))]
[JsonSerializable(typeof(TpSheetSprite))]
[JsonSerializable(typeof(TpSheetRect))]
[JsonSerializable(typeof(TpSheetSize))]
internal sealed partial class TpSheetSerializerContext : JsonSerializerContext
{
}
