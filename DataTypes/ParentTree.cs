// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

namespace SimChA.DataTypes;

// Should be refactored into a recursive form
public struct ParentTree
{
    public int RootId;
    public List<TreeNode> Nodes;
    public List<TreeEdge> Edges;
}