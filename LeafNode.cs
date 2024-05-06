internal partial class Program
{
    class LeafNode
    {
        public Dictionary<int, ReservedNode> Branch { get; set; }
        public int Level { get; set; }
        public double Cost { get; set; }

        public LeafNode? Parent = null;

        public LeafNode(Dictionary<int, ReservedNode> b, int l, double c, LeafNode? p = null)
        {
            Branch = b;
            Level = l;
            Cost = c;
            Parent = p;
        }

        public override string? ToString()
        {
            string text = "";
            foreach (var elem in Branch)
            {
                text += $"{elem.Key}\t | {elem.Value.Bit} -> {elem.Value.Budget}\n";
            }
            text += $"Cost = {Cost}\tLevel = {Level}\n";

            return text;
        }
    }
}