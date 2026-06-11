// Created by Dr. Adam Streck, 2021, adam.streck@gmail.com

namespace SMITH.DataTypes;

public class TreeNode(int id, long size, string label = "")
{
    public readonly int Id = id;
    public readonly long Size = size;
    public readonly string Label = label == "" ? id.ToString() : label;
    public List<(TreeNode child, int distance)> Children = [];
}