// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

namespace SimChA.DataTypes;

public class TreeNode
{
    public int Id;
    public long Size;
    public List<(TreeNode child, int distance)> Children;

    public TreeNode(int id, long size)
    {
        Id = id;
        Size = size;
        Children = new List<(TreeNode, int)>();
    }
}