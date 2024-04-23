namespace AppLogic
{
    class Scheme
    {
        public Dictionary<string, List<Node>> scheme { get; set;  } = new Dictionary<string, List<Node>>();
        public double Budget { get; set; } = 0;

        public double CalculatedBudget { get; set; } = 0;

        public Scheme(Dictionary<string, List<Node>> scheme, double budget)
        {
            this.scheme = scheme;
            Budget = budget;
        }

        public bool isGeneratedSchemeFromConfigUnderBudget(string binaryConfig)
        {
            int idx = 0;
            foreach (var chain in scheme.Values)
            {
                foreach (var node in chain)
                {
                    node.hasExtra = binaryConfig[idx] == '1';
                    idx++;
                }
            }
            this.CalculateBudgetForSchemeConfig();

            return CalculatedBudget < this.Budget;
        }

        public void CalculateBudgetForSchemeConfig()
        {
            CalculatedBudget = scheme.Sum(chain => chain.Value.Where(item => item.hasExtra).Sum(item => item.Cost));
        }
    }
}