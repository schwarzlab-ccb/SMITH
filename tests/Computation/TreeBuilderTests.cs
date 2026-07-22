using SMITH.Computation;
using SMITH.DataTypes;
using Xunit;

namespace SMITH.Tests.Computation;

public class TreeBuilderTests
{
    [Fact]
    public void BuildCTree_AccumulatesCloneDistancesAlongCollapsedPath()
    {
        var root = new Clone(0, -1, 0, 1.0, 0, 10, 1);
        var intermediate = new Clone(1, 0, 1, 1.0, 1, 4, 1);
        var leaf = new Clone(2, 1, 2, 1.0, 2, 7, 1);

        var tree = TreeBuilder.BuildCTree([root, intermediate, leaf], [root, leaf]);

        var edge = Assert.Single(tree.Edges);
        Assert.Equal(0, edge.SourceId);
        Assert.Equal(2, edge.TargetId);
        Assert.Equal(11, edge.Distance);
    }

    [Fact]
    public void BuildLCAT_UsesCloneDistancesForLeavesAndInternalNodes()
    {
        var root = new Clone(0, -1, 0, 1.0, 0, 10, 1);
        var internalNode = new Clone(1, 0, 1, 1.0, 1, 4, 1);
        var leftLeaf = new Clone(2, 1, 2, 1.0, 2, 7, 1);
        var rightLeaf = new Clone(3, 1, 2, 1.0, 2, 9, 1);

        var tree = TreeBuilder.BuildLCAT([root, internalNode, leftLeaf, rightLeaf], [leftLeaf, rightLeaf]);

        var leftEdge = tree.Edges.Single(e => e.TargetId == 2);
        Assert.Equal(1, leftEdge.SourceId);
        Assert.Equal(7, leftEdge.Distance);

        var rightEdge = tree.Edges.Single(e => e.TargetId == 3);
        Assert.Equal(1, rightEdge.SourceId);
        Assert.Equal(9, rightEdge.Distance);

        var internalEdge = tree.Edges.Single(e => e.TargetId == 1);
        Assert.Equal(0, internalEdge.SourceId);
        Assert.Equal(4, internalEdge.Distance);
    }
}

