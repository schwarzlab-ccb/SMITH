using SMITH.Simulation;
using SMITH.Tests.TestSupport;
using Xunit;

namespace SMITH.Tests.Simulation;

public class UtilityTests
{
    [Fact]
    public void CreateCheckpoints_RejectsZeroMinPop()
    {
        var simParams = TestHelper.LoadFixtureParams();
        simParams.Checkpoints = true;
        simParams.MinPop = 0;

        Assert.NotEmpty(simParams.SanityCheck());
        Assert.Throws<ArgumentOutOfRangeException>(() => Utility.CreateCheckpoints(simParams));
    }

    [Fact]
    public void CreateCheckpoints_DoesNotOverflowAtLongMaxValue()
    {
        var simParams = TestHelper.LoadFixtureParams();
        simParams.Checkpoints = true;
        simParams.MinPop = uint.MaxValue;
        simParams.MaxPop = long.MaxValue;

        var checkpoints = Utility.CreateCheckpoints(simParams);

        Assert.Equal(long.MaxValue, checkpoints[^1]);
        Assert.All(checkpoints, checkpoint => Assert.True(checkpoint > 0));
        Assert.Equal(checkpoints.Count, checkpoints.Distinct().Count());
    }
}
