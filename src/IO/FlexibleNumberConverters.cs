using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SMITH.IO;

// System.Text.Json rejects JSON numbers written in scientific notation (e.g. 1e9) or with a
// decimal point when the target is an integer type. These converters accept those forms by
// falling back to a floating-point read and rounding, so config values like "MaxPop": 1e9 work.
public sealed class FlexibleInt64Converter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.String
            ? (long)Math.Round(double.Parse(reader.GetString()!, CultureInfo.InvariantCulture))
            : reader.TryGetInt64(out long value) ? value : (long)Math.Round(reader.GetDouble());

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

public sealed class FlexibleInt32Converter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.String
            ? (int)Math.Round(double.Parse(reader.GetString()!, CultureInfo.InvariantCulture))
            : reader.TryGetInt32(out int value) ? value : (int)Math.Round(reader.GetDouble());

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

public sealed class FlexibleUInt32Converter : JsonConverter<uint>
{
    public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.String
            ? (uint)Math.Round(double.Parse(reader.GetString()!, CultureInfo.InvariantCulture))
            : reader.TryGetUInt32(out uint value) ? value : (uint)Math.Round(reader.GetDouble());

    public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
