// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SMITH.DataTypes;

namespace SMITH.Computation;

public static class TreeBuilder
{
    private static Dictionary<int, int> CreateParentMap(IEnumerable<Clone> subClones)
        => subClones.ToDictionary(sc => sc.CloneId, sc => sc.ParentId);

    private static Dictionary<int, int> CreateDistanceMap(IEnumerable<Clone> subClones)
        => subClones.ToDictionary(sc => sc.CloneId, sc => Convert.ToInt32(sc.Distance));

    private static ListEdge FindEdgeToParent(Dictionary<int, int> parentMap, Dictionary<int, int> distanceMap,
        List<Clone> selection, int id)
    {
        int dist = 0;
        int source = id;

        do
        {
            dist += distanceMap[source];
            source = parentMap[source];
        } while (selection.All(sc => sc.CloneId != source) && source != -1);

        return new ListEdge { Distance = dist, SourceId = source, TargetId = id };
    }

    private static List<int> FindInternalNodes(Dictionary<int, int> parentMap, List<Clone> selection)
    {
        Dictionary<int, int> internalNodes = new();

        foreach (var subClone in selection)
        {
            int curNode = parentMap[subClone.CloneId];
            while (selection.All(sc => sc.CloneId != curNode) && curNode != -1)
            {
                if (internalNodes.ContainsKey(curNode))
                {
                    internalNodes[curNode]++;
                    break;
                }

                internalNodes[curNode] = 0;
                curNode = parentMap[curNode];
            }
        }

        return internalNodes.Where(n => n.Value > 0 || n.Key == 0).Select(n => n.Key).ToList();
    }
    
    // Construct a parent tree with each child being either parent of a present predecessor, or -1 if none exists.
    public static ListTree BuildCTree(List<Clone> allSubClones, List<Clone> selection)
    {
        var parentMap = CreateParentMap(allSubClones);
        var distanceMap = CreateDistanceMap(allSubClones);
        List<ListNode> nodes = new();
        List<ListEdge> edges = new();
        int rootId = -1;

        foreach (var subClone in selection)
        {
            nodes.Add(new ListNode { Id = subClone.CloneId, Size = subClone.AliveCount });
            edges.Add(FindEdgeToParent(parentMap, distanceMap, selection, subClone.CloneId));
        }

        if (edges.Count(e => e.SourceId == -1) > 1)
        {
            nodes.Add(new ListNode { Id = -1, Size = 0 }); // Root in an abstract node since the root is missing
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

        return new ListTree { RootId = rootId, Nodes = nodes, Edges = edges };
    }

    private static ListEdge FindEdge(Dictionary<int, int> parentMap, Dictionary<int, int> distanceMap,
        List<Clone> selection, List<int> internalNodes, int id)
    {
        int dist = 0;
        int source = id;
        do
        {
            dist += distanceMap[source];
            source = parentMap[source];
        } while (source != -1 && selection.All(sc => sc.CloneId != source) && internalNodes.All(n => n != source));

        return new ListEdge { Distance = dist, SourceId = source, TargetId = id };
    }

    // Construct a parent tree with lowest common ancestor (LCA) for each pair of children
    public static ListTree BuildLCAT(IEnumerable<Clone> allSubClones, List<Clone> selection)
    {
        List<ListNode> nodes = new();
        List<ListEdge> edges = new();
        var subClones = allSubClones.ToList();
        
        var parentMap = CreateParentMap(subClones);
        var distanceMap = CreateDistanceMap(subClones);
        var internalNodes = FindInternalNodes(parentMap, selection);

        foreach (var subClone in selection)
        {
            nodes.Add(new ListNode { Id = subClone.CloneId, Size = subClone.AliveCount });
            edges.Add(FindEdge(parentMap, distanceMap, selection, internalNodes, subClone.CloneId));
        }

        foreach (int internalNode in internalNodes)
        {
            nodes.Add(new ListNode { Id = internalNode, Size = 0 });
            edges.Add(FindEdge(parentMap, distanceMap, selection, internalNodes, internalNode));
        }

        return new ListTree { RootId = 0, Nodes = nodes, Edges = edges.Where(e => e.TargetId != 0).ToList() };
    }

    private static void WalkTheTree(ListTree listTree, TreeNode currentNode)
    {
        var children = listTree.Edges.Where(e => e.SourceId == currentNode.Id).ToList();
        foreach (var child in children)
        {
            var childNode = new TreeNode(child.TargetId, listTree.Nodes.Find(node => node.Id == child.TargetId).Size);
            currentNode.Children.Add((childNode, child.Distance));
            WalkTheTree(listTree, childNode);
        }
    }
    
    public static TreeNode ListToTree(ListTree listTree)
    {
        var root = new TreeNode(listTree.RootId, listTree.Nodes.Find(n => n.Id == listTree.RootId).Size);
        WalkTheTree(listTree, root);
        return root;
    }

    private sealed record TimedNode(TreeNode Node, int Generation);

    private sealed record BranchEvent(int Generation, List<Clone> Children);

    /// <summary>
    /// Builds a Newick-ready clone tree whose branch lengths are simulation steps.
    /// At every child-appearance step, the parent clone branches into its new
    /// subclone lineage(s) and a continuation of itself.
    /// </summary>
    public static (TreeNode Tree, int RootDistance) BuildTimeTree(
        IEnumerable<Clone> allClones, ListTree cloneTree, int finalGeneration)
    {
        var clones = allClones.ToDictionary(clone => clone.CloneId);
        if (!clones.TryGetValue(cloneTree.RootId, out var rootClone))
        {
            throw new ArgumentException(
                $"Tree root {cloneTree.RootId} has no corresponding clone.");
        }

        if (finalGeneration < rootClone.FirstGen)
        {
            throw new ArgumentOutOfRangeException(nameof(finalGeneration));
        }

        var childrenByParent = cloneTree.Edges
            .GroupBy(edge => edge.SourceId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(edge => edge.TargetId).ToList());

        var root = BuildTimeLineage(
            rootClone, clones, childrenByParent, finalGeneration);
        return (root.Node, root.Generation - rootClone.FirstGen);
    }

    private static TimedNode BuildTimeLineage(
        Clone clone,
        Dictionary<int, Clone> clones,
        Dictionary<int, List<int>> childrenByParent,
        int finalGeneration)
    {
        var events = childrenByParent.GetValueOrDefault(clone.CloneId, [])
            .Select(childId =>
            {
                if (!clones.TryGetValue(childId, out var child))
                {
                    throw new ArgumentException(
                        $"Tree node {childId} has no corresponding clone.");
                }

                var branchEntry = FindBranchEntryClone(
                    clones, clone.CloneId, childId);
                return (Child: child, Generation: branchEntry.FirstGen);
            })
            .GroupBy(branch => branch.Generation)
            .OrderBy(group => group.Key)
            .Select(group => new BranchEvent(
                group.Key,
                group.Select(branch => branch.Child)
                    .OrderBy(child => child.CloneId)
                    .ToList()))
            .ToList();

        return BuildTimeContinuation(
            clone, events, 0, clones, childrenByParent, finalGeneration);
    }

    private static TimedNode BuildTimeContinuation(
        Clone clone,
        List<BranchEvent> events,
        int eventIndex,
        Dictionary<int, Clone> clones,
        Dictionary<int, List<int>> childrenByParent,
        int finalGeneration)
    {
        if (eventIndex == events.Count)
        {
            return new TimedNode(
                new TreeNode(clone.CloneId, clone.AliveAtGen(finalGeneration)),
                finalGeneration);
        }

        var branchEvent = events[eventIndex];
        if (branchEvent.Generation < clone.FirstGen
            || branchEvent.Generation > finalGeneration)
        {
            throw new InvalidOperationException(
                $"Clone {clone.CloneId} has a branch outside its simulation lifetime.");
        }

        var eventNode = new TreeNode(
            clone.CloneId, clone.AliveAtGen(branchEvent.Generation));
        foreach (var child in branchEvent.Children)
        {
            var childRoot = BuildTimeLineage(
                child, clones, childrenByParent, finalGeneration);
            int distance = childRoot.Generation - branchEvent.Generation;
            if (distance < 0)
            {
                throw new InvalidOperationException(
                    $"Clone {child.CloneId} precedes its parent branch.");
            }

            eventNode.Children.Add((childRoot.Node, distance));
        }

        var continuation = BuildTimeContinuation(
            clone,
            events,
            eventIndex + 1,
            clones,
            childrenByParent,
            finalGeneration);
        eventNode.Children.Add((
            continuation.Node,
            continuation.Generation - branchEvent.Generation));

        return new TimedNode(eventNode, branchEvent.Generation);
    }

    private static Clone FindBranchEntryClone(
        Dictionary<int, Clone> clones, int parentId, int targetId)
    {
        var current = clones[targetId];
        var visited = new HashSet<int>();
        while (current.ParentId != parentId)
        {
            if (!visited.Add(current.CloneId)
                || current.ParentId == -1
                || !clones.TryGetValue(current.ParentId, out current))
            {
                throw new ArgumentException(
                    $"Tree node {targetId} is not a descendant of clone {parentId}.");
            }
        }

        return current;
    }
}
