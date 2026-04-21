using SMITH.Computation;
using SMITH.DataTypes;
using Xunit;

namespace SMITH.Tests.Computation;

public class TreeBuilderTests
{
    [Fact]
    public void ConvertToBifrucatingNodes_MakesMultifurcationIntoAppearanceOrderedChain()
    {
        var root = new TreeNode(0, 10);
        var childA = new TreeNode(1, 1);
        var childB = new TreeNode(2, 1);
        var childC = new TreeNode(3, 1);

        // Intentionally shuffled input order.
        root.Children.Add((childC, 9));
        root.Children.Add((childA, 7));
        root.Children.Add((childB, 3));

        var firstGen = new Dictionary<int, int>
        {
            [0] = 0,
            [1] = 30,
            [2] = 10,
            [3] = 20
        };

        TreeBuilder.ConvertToBifrucatingNodes(firstGen, root);

        Assert.Equal(2, root.Children.Count);
        Assert.Equal(2, root.Children[0].child.Id);
        Assert.Equal(1, root.Children[0].distance);
        Assert.Equal(0, root.Children[1].distance);

        var self1 = root.Children[1].child;
        Assert.Equal(2, self1.Children.Count);
        Assert.Equal(3, self1.Children[0].child.Id);
        Assert.Equal(1, self1.Children[0].distance);
        Assert.Equal(0, self1.Children[1].distance);

        var self2 = self1.Children[1].child;
        Assert.Single(self2.Children);
        Assert.Equal(1, self2.Children[0].child.Id);
        Assert.Equal(1, self2.Children[0].distance);
    }
}

