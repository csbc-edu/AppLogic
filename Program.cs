using System.Numerics;

namespace AppLogic
{
    public class Node
    {
        public long ID { get; set; }
        public double LambdaT { get; set; }
        public double LambdaTau { get; set; }
        public double Cost { get; set; }

        public bool hasExtra { get; set; } = false;

        public double Reliability = 0;

        public Node(long iD, double lambdaT, double lambdaTau, double cost)
        {
            ID = iD;
            LambdaT = lambdaT;
            LambdaTau = lambdaTau;
            Cost = cost;
        }

        public double CalculateNodeReliability(int xij)
        {
            const int TRIALS = 1000;
            Random rand_t = new Random(), rand_tau = new Random();
            double[] timesToLive = new double[TRIALS];

            for (int i = 0; i < TRIALS; i++)
            {
                double TimeToLive = this.LambdaT * Math.Log(1 / (1 - rand_t.NextDouble()));
                double TimeToLiveExtra = xij * this.LambdaTau * Math.Log(1 / (1 - rand_tau.NextDouble()));
                timesToLive[i] = TimeToLive + TimeToLiveExtra;
            }

            return timesToLive.Average();
        }
    }

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
    internal class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, List<Node>> scheme = new Dictionary<string, List<Node>>()
            {
                ["1"] = new List<Node>() {
                            new Node(1, 20, 40, 30),
                            new Node(2, 10, 10, 20),
                            new Node(3, 20, 10, 10),
                            new Node(4, 20, 10, 40),
                        },
                ["2"] = new List<Node>() {
                            new Node(5, 30, 20, 30),
                            new Node(6, 20, 10, 20),
                            new Node(7, 40, 20, 50),
                            new Node(8, 20, 20, 20),
                            new Node(9, 10, 30, 30),
                            new Node(10, 10, 30, 10),
                        },
                ["3"] = new List<Node>() {
                            new Node(11, 20, 10, 20),
                            new Node(12, 10, 20, 30)
                        },
                ["4"] = new List<Node>() {
                            new Node(13, 10, 10, 20),
                            new Node(14, 10, 10, 10),
                            new Node(15, 20, 30, 30),
                            new Node(16, 10, 30, 20),
                            new Node(17, 30, 30, 10),
                },
                ["5"] = new List<Node>() {
                            new Node(18, 40, 30, 20),
                },
                ["6"] = new List<Node>() {
                            new Node(19, 20, 30, 30),
                            new Node(20, 40, 20, 50),
                            new Node(21, 20, 20, 20),
                            new Node(22, 10, 30, 30),
                }
            };
           
            double C = 120.0;      // all = 550      
            var myScheme = new Scheme(scheme, C);
            int numberOfNodes = myScheme.scheme.Values.Sum(list => list.Count);

            int numberOfConfiguration = 0;

            for (var config = new BigInteger(Math.Pow(2, numberOfNodes) - 1); config >= new BigInteger(1); config--)
            {
                // get binary config
                string binaryConfig = config.ToBinaryString().TrimStart('0'); // delete sign bit
                string leadingZeros = new string('0', numberOfNodes - binaryConfig.Length);
                binaryConfig = string.Concat(Enumerable.Reverse(leadingZeros + binaryConfig));

                numberOfConfiguration++;
                // if budget is ok
                if (myScheme.isGeneratedSchemeFromConfigUnderBudget(binaryConfig))
                {               
                    // calculate min reliabilities for chains
                    List<Node> minReliableNodesInChains =  new List<Node>();

                    foreach (var chain in myScheme.scheme.Values)
                    {                       
                        chain.AsParallel().ForAll(node => node.Reliability = node.CalculateNodeReliability(node.hasExtra ? 1 : 0));
                        
                        minReliableNodesInChains.Add(chain.MinBy(node => node.Reliability)!); // min for serial part
                    }

                    // calculate max chain reliability for scheme
                    var schemeReliability = minReliableNodesInChains.MaxBy(x => x.Reliability)!; // max for parallel part
                    Console.WriteLine("#config = {0, 8} | ID = {1}\t | Reliability = {2:N8}\t | Configuration = {3} | Budget = {4}", numberOfConfiguration, schemeReliability.ID, schemeReliability.Reliability, binaryConfig, myScheme.CalculatedBudget);
                }
            }
        }
    }
}