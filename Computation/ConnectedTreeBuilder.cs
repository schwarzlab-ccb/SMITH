// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SimChA.DataTypes;

namespace SimChA.Computation;

public static class ConnectedTreeBuilder
{
    public static Dictionary<int, int> CreateParentMap(IEnumerable<SubClone> subClones)
        => subClones.ToDictionary(sc => sc.CloneId, sc => sc.ParentId);

    private static TreeEdge FindEdgeToParent(Dictionary<int, int> parentMap, List<SubClone> selection, int id)
    {
        int dist = 0;
        int source = id;

        do
        {
            dist++;
            source = parentMap[source];
        } while (selection.All(sc => sc.CloneId != source) && source != -1);

        return new TreeEdge { Distance = dist, SourceId = source, TargetId = id };
    }

    // Construct a parent tree with each child being either parent of a present predecessor, or -1 if none exists.
    public static ParentTree BuildTree(IEnumerable<SubClone> allSubClones, List<SubClone> selection)
    {
        var parentMap = CreateParentMap(allSubClones);
        List<TreeNode> nodes = new();
        List<TreeEdge> edges = new();
        int rootId = -1;

        foreach (var subClone in selection)
        {
            nodes.Add(new TreeNode { Id = subClone.CloneId, Size = subClone.AliveCount });
            edges.Add(FindEdgeToParent(parentMap, selection, subClone.CloneId));
        }

        if (edges.Count(e => e.SourceId == -1) > 1)
        {
            nodes.Add(new TreeNode { Id = -1, Size = 0 }); // Root in an abstract node since the root is missing
            rootId = -1;
        }
        else
        {
            var firstEdge = edges.Find(e => e.SourceId == -1);
            if (firstEdge != null)
            {
                edges.Remove(firstEdge);
                rootId = firstEdge.TargetId;
            }
        }

        return new ParentTree { RootId = rootId, Nodes = nodes, Edges = edges };
    }
}