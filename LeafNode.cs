internal partial class Program
{
    class LeafNode
    {
        public Dictionary<int, ReservedNode> Branch { get; set; }
        public int Level { get; set; }
        public double TotalCost { get; set; }

        public LeafNode? Parent = null;

        public LeafNode(Dictionary<int, ReservedNode> b, int l, double c, LeafNode? p = null)
        {
            Branch = copyBranch(b);
            Level = l;
            TotalCost = c;
            Parent = p;
        }

        public LeafNode(LeafNode node)
        {
            Branch = copyBranch(node.Branch);
            Level = node.Level;
            TotalCost = node.TotalCost;
            Parent = node.Parent;
        }

        public Dictionary<int, ReservedNode> copyBranch(Dictionary<int, ReservedNode> dict)
        {
            var res = new Dictionary<int, ReservedNode>();
            foreach (var b in dict)
            {
                var value = dict[b.Key];
                res[b.Key] = new ReservedNode()
                {
                    Bit = value.Bit,
                    Budget = value.Budget,
                };
            }
            return res;
        }
        /*
        public override string? ToString()
        {
            string text = "";
            foreach (var elem in Branch)
            {
                text += $"{elem.Key}\t | {elem.Value.Bit} -> {elem.Value.Budget}\n";
            }
            text += $"TotalCost = {TotalCost}\tLevel = {Level}\n";

            return text;
        }*/
    }
}