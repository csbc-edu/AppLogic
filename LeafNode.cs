using AppLogic;

internal partial class Program
{
    class LeafNode
    {
        public Dictionary<Key, ReservedNode> Branch { get; set; }
        public int Level { get; set; }
        public double TotalCost { get; set; }

        public LeafNode(Dictionary<Key, ReservedNode> b, int l, double c)
        {
            Branch = copyBranch(b);
            Level = l;
            TotalCost = c;
        }

        public LeafNode(LeafNode node)
        {
            Branch = copyBranch(node.Branch);
            Level = node.Level;
            TotalCost = node.TotalCost;
        }

        public Dictionary<Key, ReservedNode> copyBranch(Dictionary<Key, ReservedNode> dict)
        {
            var res = new Dictionary<Key, ReservedNode>();
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
        
        public override string? ToString()
        {
            string text = "";
            foreach (var elem in Branch)
            {
                text += $"{(elem.Key.Id, elem.Key.Bit)}\n | {elem.Value.Bit} -> {elem.Value.Budget}\n";
            }
            text += $"TotalCost = {TotalCost}\tLevel = {Level}\n";

            return text;
        }
    }
}