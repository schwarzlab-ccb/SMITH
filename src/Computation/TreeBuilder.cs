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

    public static int ConvertToBinaryNodes(Dictionary<int, int> firstGen, TreeNode tree, int minFreeId)
    {
        // Keep LCA if empty
        if (tree.Children.Count == 2 && tree.Size == 0)
        {
            minFreeId = ConvertToBinaryNodes(firstGen, tree.Children[0].child, minFreeId + 1);
            minFreeId = ConvertToBinaryNodes(firstGen, tree.Children[1].child, minFreeId + 1);
        }
        // Split the self and add oldest child
        else if (tree.Children.Count > 1)
        {
            tree.Children.Sort((c, d) => firstGen[c.child.Id]);
            var firstChild = tree.Children[0];
            var restChildren = tree.Children.Skip(1).ToList();
            var copy = new TreeNode(minFreeId, 0)
            {
                Label = tree.Label,
                Children = restChildren
            };
            tree.Children = new List<(TreeNode child, int dist)> { (firstChild.child, firstChild.distance), (copy, 0) };
            minFreeId = ConvertToBinaryNodes(firstGen, tree.Children[0].child, minFreeId + 1);
            minFreeId = ConvertToBinaryNodes(firstGen, tree.Children[1].child, minFreeId + 1);
        }
        // Continue the traversal
        else if (tree.Children.Count == 1)
        {
            minFreeId = ConvertToBinaryNodes(firstGen, tree.Children[0].child, minFreeId + 1);
        }
        return minFreeId;
    }
}