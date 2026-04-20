// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

namespace SMITH.DataTypes;

public class TreeNode
{
    public int Id;
    public long Size;
    public string Label;
    public List<(TreeNode child, int distance)> Children;

    public TreeNode(int id, long size, string label = "")
    {
        Id = id;
        Size = size;
        Label = label == "" ? id.ToString() : label;
        Children = new List<(TreeNode, int)>();
    }
}