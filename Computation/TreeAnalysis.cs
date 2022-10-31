using SimChA.DataTypes;

namespace SimChA.Computation;

public static class TreeAnalysis
{
    private static long SubtreeCellCount(ListTree listTree, Dictionary<int, long> knownSizes, ListNode subtreeRoot)
    {
        if (knownSizes.ContainsKey(subtreeRoot.Id))
        {
            return knownSizes[subtreeRoot.Id];
        }
        long size = subtreeRoot.Size + listTree.Edges
            .Where(e => e.SourceId == subtreeRoot.Id)
            .Select(e => SubtreeCellCount(listTree, knownSizes, listTree.Nodes.Find(n => n.Id == e.TargetId)))
            .Sum();
        knownSizes[subtreeRoot.Id] = size; 
        return size;
    }

    public static Dictionary<int, long> ComputeCCF(ListTree listTree)
    {
        var knownSizes = new Dictionary<int, long>();
        var CCF = new Dictionary<int, long>();
        foreach (var node in listTree.Nodes)
        {
            long size = SubtreeCellCount(listTree, knownSizes, node);
            if (size > 0)
            {
                CCF[node.Id] = size;
            }
        }
        return CCF;
    }

    private static int CountNodes(Dictionary<int, List<int>> branches, TreeSizeData data, int id, int depth)
    {
        var children = branches[id];
        if (children.Any())
        {
            data.childCount += children.Count;
            return children.Select(c => CountNodes(branches, data, c, depth + 1)).Max();
        }

        data.leafCount += 1;
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
        int leafCount = data.leafCount;
        float branching = data.childCount / (float)leafCount;
        return (nodeCount, leafCount, depth, branching);
    }

    public static float ComputeTreeBalance(int leafCount, ListTree listTree)
    {
        if (leafCount == 1)
        {
            return 0;
        }

        float treeBalance = 0;
        long Sdash_i_sum = 0;
        var subtreeCount = ComputeCCF(listTree);
        var branches = TreeToBranches(listTree);

        foreach (var node in listTree.Nodes.Where(n => branches[n.Id].Count >= 2))
        {
            int nChildren = branches[node.Id].Count;
            long S_i = subtreeCount[node.Id];
            if (S_i == 0)
            {
                continue;
            }

            long Sdash_i = S_i - node.Size;
            Sdash_i_sum += Sdash_i;

            float W_i = branches[node.Id].Select(b => (float)subtreeCount[b] / Sdash_i)
                .Where(p => p > 0)
                .Select(p => -1 * p * (float)Math.Log(p) / (float)Math.Log(nChildren))
                .Sum();

            treeBalance += Sdash_i * Sdash_i / S_i * W_i;
        }

        return treeBalance / Sdash_i_sum;
    }


    public static double ComputeClonalDiversity(List<SubClone> subClones)
    {
        long totalPop = subClones.Select(clone => clone.AliveCount).Sum();
        double clonalDiversity = 1 / subClones.Select(clone => Math.Pow((float)clone.AliveCount / totalPop, 2)).Sum();
        return clonalDiversity;
    }

    public static double ComputeMeanDriversPerCell(List<SubClone> subClones)
    {
        return subClones.Select(clone => (double)clone.AliveCount * clone.NumberDrivers).Sum()
               / subClones.Select(clone => (double)clone.AliveCount).Sum();
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
        internal int childCount;
        internal int leafCount;
    }
}