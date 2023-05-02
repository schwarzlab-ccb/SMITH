using System.Text.Json.Serialization;

namespace SMITH.DataTypes;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FitnessDistType
{
    Constant,
    Normal,
    Exponential,
    Uniform
}