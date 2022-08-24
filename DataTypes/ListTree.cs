// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

namespace SimChA.DataTypes;

// Should be refactored into a recursive form
public struct ListTree
{
    public int RootId;
    public List<ListNode> Nodes;
    public List<ListEdge> Edges;
}