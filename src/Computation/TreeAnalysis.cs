using SMITH.DataTypes;

namespace SMITH.Computation;

public static class TreeAnalysis
{
    private static long SubtreeCellCount(ListTree listTree, Dictionary<int, long> knownSizes, ListNode subtreeRoot)
    {
        if (!knownSizes.TryGetValue(subtreeRoot.Id, out long size))
        {
            size = subtreeRoot.Size + listTree.Edges
                .Where(e => e.SourceId == subtreeRoot.Id)
                .Select(e => SubtreeCellCount(listTree, knownSizes, listTree.Nodes.Find(n => n.Id == e.TargetId)))
                .Sum();
            knownSizes[subtreeRoot.Id] = size; 
        }
        return size;
    }

    public static Dictionary<int, long> ComputeCCF(ListTree listTree)
    {
        var knownSizes = new Dictionary<int, long>();
        return listTree.Nodes.ToDictionary(n => n.Id, n => SubtreeCellCount(listTree, knownSizes, n));
    }

    private static int CountNodes(Dictionary<int, List<int>> branches, TreeSizeData data, int id, int depth)
    {
        var children = branches[id];
        if (children.Any())
        {
            data.ChildCount += children.Count;
            return children.Select(c => CountNodes(branches, data, c, depth + 1)).Max();
        }

        data.LeafCount += 1;
        return depth;
    }

    // Returns number of nodes, number of leafs, depth, mean child count
    public static (int, int, int, float) ComputeTreeSize(ListTree listTree)
    {
        if (listTree.Nodes.Count == 0)
        {
            return (0, 0, 0, 0);
        }

        TreeSizeData data = new();
        var branches = TreeToBranches(listTree);
        int depth = CountNodes(branches, data, listTree.RootId, 0);
        int nodeCount = listTree.Nodes.Count;
        int leafCount = data.LeafCount;
        float branching = data.ChildCount / (float)leafCount;
        return (nodeCount, leafCount, depth, branching);
    }

    public static float ComputeTreeBalance(int leafCount, ListTree listTree, Dictionary<int, long> CCF)
    {
        if (leafCount <= 1)
        {
            return 0;
        }

        float treeBalance = 0;
        long Sdash_i_sum = 0;
        var branches = TreeToBranches(listTree);

        foreach (var node in listTree.Nodes.Where(n => branches[n.Id].Count >= 2))
        {
            int nChildren = branches[node.Id].Count;
            long S_i = CCF[node.Id];
            if (S_i == 0)
            {
                continue;
            }

            long Sdash_i = S_i - node.Size;
            Sdash_i_sum += Sdash_i;

            float W_i = branches[node.Id].Select(b => (float) CCF[b] / Sdash_i)
                .Where(p => p > 0)
                .Select(p => -1 * p * (float)Math.Log(p) / (float)Math.Log(nChildren))
                .Sum();

            treeBalance += Sdash_i * Sdash_i / S_i * W_i;
        }

        return Sdash_i_sum > 0 ? treeBalance / Sdash_i_sum : 0;
    }


    public static double ComputeClonalDiversity(List<Clone> subClones)
    {
        long totalPop = subClones.Select(clone => clone.AliveCount).Sum();
        if (totalPop <= 0)
        {
            return 0;
        }

        double concentration = subClones
            .Select(clone => Math.Pow((double)clone.AliveCount / totalPop, 2))
            .Sum();
        return concentration > 0 ? 1 / concentration : 0;
    }

    public static double ComputeMeanDriversPerCell(List<Clone> subClones)
    {
        double totalPop = subClones.Select(clone => (double)clone.AliveCount).Sum();
        return totalPop > 0
            ? subClones.Select(clone => (double)clone.AliveCount * clone.DriverCount).Sum() / totalPop
            : 0;
    }

    private static Dictionary<int, List<int>> TreeToBranches(ListTree pt)
    {
        Dictionary<int, List<int>> branches = new();
        foreach (var node in pt.Nodes)
        {
            var targets = pt.Edges.Where(e => e.SourceId == node.Id).Select(e => e.TargetId).ToList();
            branches.Add(node.Id, targets);
        }

        return branches;
    }

    private class TreeSizeData
    {
        internal int ChildCount;
        internal int LeafCount;
    }
}
