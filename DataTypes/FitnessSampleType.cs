using System.Text.Json.Serialization;

namespace SMITH.DataTypes;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FitnessSampleType
{
    Constant,
    Normal,
    Exponential,
    Uniform
}