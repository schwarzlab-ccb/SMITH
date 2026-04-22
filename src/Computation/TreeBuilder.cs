// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

using SMITH.DataTypes;

namespace SMITH.Computation;

public static class TreeBuilder
{
    public static Dictionary<int, int> CreateParentMap(IEnumerable<Clone> subClones)
        => subClones.ToDictionary(sc => sc.CloneId, sc => sc.ParentId);
    
    public static Dictionary<int, int> CountFirstGent(IEnumerable<Clone> subClones)
        => subClones.ToDictionary(sc => sc.CloneId, sc => sc.FirstGen);

    private static ListEdge FindEdgeToParent(Dictionary<int, int> parentMap, List<Clone> selection, int id)
    {
        int dist = 0;
        int source = id;

        do
        {
            dist++;
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
        List<ListNode> nodes = new();
        List<ListEdge> edges = new();
        int rootId = -1;

        foreach (var subClone in selection)
        {
            nodes.Add(new ListNode { Id = subClone.CloneId, Size = subClone.AliveCount });
            edges.Add(FindEdgeToParent(parentMap, selection, subClone.CloneId));
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

    private static ListEdge FindEdge(Dictionary<int, int> parentMap, List<Clone> selection, List<int> internalNodes, int id)
    {
        int dist = 0;
        int source = id;
        do
        {
            dist++;
            source = parentMap[source];
        } while (source != -1 && selection.All(sc => sc.CloneId != source) && internalNodes.All(n => n != source));

        return new ListEdge { Distance = dist, SourceId = source, TargetId = id };
    }

    // Construct a parent tree with lowest common ancestor (LCA) for each pair of children
    public static ListTree BuildLCAT(IEnumerable<Clone> allSubClones, List<Clone> selection)
    {
        List<ListNode> nodes = new();
        List<ListEdge> edges = new();
        
        var parentMap = CreateParentMap(allSubClones);
        var internalNodes = FindInternalNodes(parentMap, selection);

        foreach (var subClone in selection)
        {
            nodes.Add(new ListNode { Id = subClone.CloneId, Size = subClone.AliveCount });
            edges.Add(FindEdge(parentMap, selection, internalNodes, subClone.CloneId));
        }

        foreach (int internalNode in internalNodes)
        {
            nodes.Add(new ListNode { Id = internalNode, Size = 0 });
            edges.Add(FindEdge(parentMap, selection, internalNodes, internalNode));
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

    public static TreeNode CloneTree(TreeNode tree)
    {
        var copy = new TreeNode(tree.Id, tree.Size, tree.Label);
        foreach (var (child, distance) in tree.Children)
        {
            copy.Children.Add((CloneTree(child), distance));
        }

        return copy;
    }

    private static int AppearanceOrder(Dictionary<int, int> firstGen, int cloneId)
        => firstGen.TryGetValue(cloneId, out int value) ? value : int.MaxValue;

    private static int AppearanceStep(Dictionary<int, int> firstGen, int cloneId)
    {
        if (cloneId == 0)
        {
            return 0;
        }

        return firstGen.TryGetValue(cloneId, out int value) ? value : 0;
    }

    public static void LabelTreeNodesWithAppearance(Dictionary<int, int> firstGen, TreeNode tree)
    {
        tree.Label = $"{tree.Id}-{AppearanceStep(firstGen, tree.Id)}";
        foreach (var (child, _) in tree.Children)
        {
            LabelTreeNodesWithAppearance(firstGen, child);
        }
    }

    private static int FindMaxNodeId(TreeNode tree)
    {
        int maxId = tree.Id;
        foreach (var (child, _) in tree.Children)
        {
            maxId = Math.Max(maxId, FindMaxNodeId(child));
        }

        return maxId;
    }

    private static int ConvertToBifrucatingNodes(Dictionary<int, int> firstGen, TreeNode tree, int nextFreeId)
    {
        if (tree.Children.Count > 1)
        {
            var orderedChildren = tree.Children
                .OrderBy(c => AppearanceOrder(firstGen, c.child.Id))
                .ThenBy(c => c.child.Id)
                .ToList();

            if (orderedChildren.Count > 2)
            {
                // Convert multifurcation into a chain: (child:1, self:0) in appearance order.
                TreeNode chainNode = tree;
                for (int i = 0; i < orderedChildren.Count; i++)
                {
                    var child = orderedChildren[i].child;
                    bool isLast = i == orderedChildren.Count - 1;
                    if (isLast)
                    {
                        chainNode.Children = new List<(TreeNode child, int distance)> { (child, 1) };
                        break;
                    }

                    var selfNode = new TreeNode(nextFreeId++, 0, tree.Label);
                    chainNode.Children = new List<(TreeNode child, int distance)> { (child, 1), (selfNode, 0) };
                    chainNode = selfNode;
                }
            }
            else
            {
                tree.Children = orderedChildren;
            }
        }

        foreach (var (child, _) in tree.Children)
        {
            nextFreeId = ConvertToBifrucatingNodes(firstGen, child, nextFreeId);
        }

        return nextFreeId;
    }

    public static void ConvertToBifrucatingNodes(Dictionary<int, int> firstGen, TreeNode tree)
    {
        int nextFreeId = FindMaxNodeId(tree) + 1;
        ConvertToBifrucatingNodes(firstGen, tree, nextFreeId);
    }

    public static int ConvertToBinaryNodes(Dictionary<int, int> firstGen, TreeNode tree, int minFreeId)
    {
        return ConvertToBifrucatingNodes(firstGen, tree, minFreeId);
    }
}