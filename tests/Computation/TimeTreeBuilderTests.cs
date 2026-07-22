using SMITH.Computation;
using SMITH.DataTypes;
using Xunit;

namespace SMITH.Tests.Computation;

public class TimeTreeBuilderTests
{
    [Fact]
    public void BuildTimeTree_UsesStepsSelfContinuationsAndSameStepMultifurcation()
    {
        var root = GrowingClone(0, -1, firstGen: 0, 10, 20, 30, 40, 50, 60);
        var childA = GrowingClone(1, 0, firstGen: 2, 2, 3, 4, 5);
        var childB = GrowingClone(2, 0, firstGen: 4, 2, 3);
        var childC = GrowingClone(3, 0, firstGen: 4, 2, 3);
        var listTree = new ListTree
        {
            RootId = 0,
            Nodes =
            [
                new ListNode { Id = 0, Size = 60 },
                new ListNode { Id = 1, Size = 5 },
                new ListNode { Id = 2, Size = 3 },
                new ListNode { Id = 3, Size = 3 }
            ],
            Edges =
            [
                new ListEdge { SourceId = 0, TargetId = 3, Distance = 1 },
                new ListEdge { SourceId = 0, TargetId = 1, Distance = 1 },
                new ListEdge { SourceId = 0, TargetId = 2, Distance = 1 }
            ]
        };

        var (tree, rootDistance) = TreeBuilder.BuildTimeTree(
            [root, childA, childB, childC], listTree, finalGeneration: 6);

        Assert.Equal(2, rootDistance);
        Assert.Equal(0, tree.Id);
        Assert.Equal(20, tree.Size);
        Assert.Equal(2, tree.Children.Count);
        Assert.Equal((1, 5, 4), NodeSummary(tree.Children[0]));

        var secondEvent = tree.Children[1];
        Assert.Equal(2, secondEvent.distance);
        Assert.Equal(0, secondEvent.child.Id);
        Assert.Equal(40, secondEvent.child.Size);
        Assert.Equal(3, secondEvent.child.Children.Count);
        Assert.Equal((2, 3, 2), NodeSummary(secondEvent.child.Children[0]));
        Assert.Equal((3, 3, 2), NodeSummary(secondEvent.child.Children[1]));
        Assert.Equal((0, 60, 2), NodeSummary(secondEvent.child.Children[2]));
    }

    [Fact]
    public void BuildTimeTree_UsesFirstRealCloneOnCollapsedBranch()
    {
        var root = GrowingClone(0, -1, firstGen: 0, 10, 20, 30, 40, 50, 60);
        var hidden = GrowingClone(1, 0, firstGen: 3, 2, 3, 4);
        var visible = GrowingClone(2, 1, firstGen: 5, 2);
        var listTree = new ListTree
        {
            RootId = 0,
            Nodes =
            [
                new ListNode { Id = 0, Size = 60 },
                new ListNode { Id = 2, Size = 2 }
            ],
            Edges =
            [
                new ListEdge { SourceId = 0, TargetId = 2, Distance = 2 }
            ]
        };

        var (tree, rootDistance) = TreeBuilder.BuildTimeTree(
            [root, hidden, visible], listTree, finalGeneration: 6);

        Assert.Equal(3, rootDistance);
        Assert.Equal(30, tree.Size);
        Assert.Equal(2, tree.Children.Count);
        Assert.Equal((2, 2, 3), NodeSummary(tree.Children[0]));
        Assert.Equal((0, 60, 3), NodeSummary(tree.Children[1]));
    }

    private static Clone GrowingClone(
        int id, int parentId, int firstGen, params long[] laterPopulations)
    {
        var clone = new Clone(id, parentId, firstGen, 1.0, 0, 1, 1);
        foreach (long population in laterPopulations)
        {
            clone.NewGen(population, 0, 0);
        }

        return clone;
    }

    private static (int Id, long Size, int Distance) NodeSummary(
        (TreeNode child, int distance) edge)
        => (edge.child.Id, edge.child.Size, edge.distance);
}
