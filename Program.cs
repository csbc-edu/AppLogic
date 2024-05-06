using AppLogic;
using System;
using System.Numerics;
using System.Reflection.Emit;
using System.Text.Encodings.Web;

internal partial class Program
{
    static Node? CalculateRelibilityForConfig(Scheme scheme, Dictionary<int, ReservedNode> branch, int numberOfNodes)
    {
        while (true)
        {
            string binaryConfig = string.Join("", GenerateRandomConfigWithFixedBits(branch, numberOfNodes));

            if (scheme.isGeneratedSchemeFromConfigUnderBudget(binaryConfig))
            {
                // calculate min reliabilities for chains
                List<Node> minReliableNodesInChains = new List<Node>();

                foreach (var chain in scheme.scheme.Values)
                {
                    minReliableNodesInChains.Add(chain.MinBy(node =>
                    {
                        var nodeIndex = chain.IndexOf(node);
                        var bit = binaryConfig.ToArray()[nodeIndex];
                        //chain[nodeIndex].hasExtra = Convert.ToBoolean(bit-'0');
                        node.ReliabilityValue = bit == '0' ? node.Reliability : node.ReliabilityExtra;
                        return node.ReliabilityValue;
                    })!); // min for serial part
                }

                // calculate max chain reliability for scheme
                var schemeReliability = minReliableNodesInChains.MaxBy(x => x.ReliabilityValue)!; // max for parallel part

                return schemeReliability;
            }
        }
    }

    static LeafNode? createBranch(LeafNode parentBranch, int index, int bit, double cost, double C)
    {
        if (parentBranch.Cost + cost < C)
        {
            var updatedBranch = parentBranch;
            updatedBranch.Branch[index] = new ReservedNode()
            {
                Bit = bit,
                Budget = cost
            };
            updatedBranch.Cost += cost;
            updatedBranch.Level++;
            updatedBranch.Parent = parentBranch;

            return updatedBranch;
        }
        return null;
    }
    static int[] GenerateRandomConfigWithFixedBits(Dictionary<int, ReservedNode> fixedBits, int numberOfBits)
    {
        int[] res = new int[numberOfBits];
        var indexes = new HashSet<int>(Enumerable.Range(0, numberOfBits)).Except(fixedBits.Keys);
        var r = new Random();

        foreach (int bitIndex in indexes)
        {
            res[bitIndex] = r.Next(0, 2);
        }
        
        return res;
    }

    private const string ResDir = "./level-logs/";
    static void writeList(List<LeafNode> list, int level)
    {
        var logFilePath = Path.Combine(ResDir, $"level-{level}.txt");

        var texts = new List<string>();
        
        foreach (var leaf in list)
        {
            if (leaf.Branch != null)
            {            
                texts.Add(leaf.ToString()!);
            }           
        }
       
        File.WriteAllText(logFilePath, string.Join("\n", texts));
    }

    private static void Main(string[] args)
    {
        if (Directory.Exists(ResDir))
        {
            Directory.Delete(ResDir, true);
        }

        Directory.CreateDirectory(ResDir);
        
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

        double C = 200.0;      // all = 550      
        var myScheme = new Scheme(scheme, C);
        int numberOfNodes = myScheme.scheme.Values.Sum(list => list.Count);

        var r = new Random();

        foreach (var chain in myScheme.scheme.Values)
            chain.AsParallel().ForAll(node =>
            {
                var randVal = r.NextDouble();
                node.CalculateNodeReliability(randVal);
            });

        
        var fixedBits = Enumerable.Range(0, numberOfNodes)
                                        .Take(numberOfNodes)
                                        .OrderBy(i => Guid.NewGuid())
                                        .ToList();
        
        fixedBits.ForEach(x => Console.Write($"{x}  "));// (fixedBits.ToString());
        List<LeafNode> branchList = new List<LeafNode>();

        Console.WriteLine();

        int currentLevel = 0;
        int nextNodeIdx = 0;
        // adds fictitious node to start iterations
        branchList.Add(new LeafNode(new Dictionary<int, ReservedNode>(), currentLevel, c:0, null));

        const int TRIALS = 1000;
        double? lowerReliabilityEstimate0 = null, lowerReliabilityEstimate1 = null;
        double? upperReliabilityEstim0 = null, upperReliabilityEstim1 = null;
        var parentNode = branchList.LastOrDefault();
        currentLevel = 1;
        for (nextNodeIdx = 0; nextNodeIdx < fixedBits.Count; nextNodeIdx++)
        {
            //Console.WriteLine($"{currentLevel} -> {parentNode!.ToString()}");
            if (parentNode is not null)
            {
                while (parentNode is not null && parentNode.Level != currentLevel - 1)
                {
                    Console.WriteLine($"{currentLevel} -> {parentNode.ToString()}");
                    parentNode = parentNode.Parent;
                }
                
                // find leaf that could be replaced (parent)                       
                currentLevel = parentNode!.Level;                   
                              
            }
            /*
            else
            {
                writeList(branchList, currentLevel);
            }*/
                


            
           
            if (parentNode is not null)
            {
                // create branch with next 0
                var branch_0 = createBranch(parentNode, fixedBits[nextNodeIdx], 0, 0, C);
                if (branch_0 is not null)  // calculate estimates for branch+0
                {
                    lowerReliabilityEstimate0 = CalculateRelibilityForConfig(myScheme!, branch_0.Branch, numberOfNodes)!.ReliabilityValue;

                    List<double> reliabilities = new List<double>();
                    for (int i = 0; i < TRIALS; i++)
                    {
                        var node = CalculateRelibilityForConfig(myScheme!, branch_0.Branch, numberOfNodes);
                        if (node is not null)
                        {
                            reliabilities.Add(node.ReliabilityValue);
                        }
                    }

                    upperReliabilityEstim0 = reliabilities.Average();
                }

                // try to create branch with next 1
                var additionalCost = myScheme!.scheme.Values.SelectMany(x => x).Where(x => x.ID == fixedBits[nextNodeIdx] + 1).First().Cost;

                var branch_1 = createBranch(parentNode, fixedBits[nextNodeIdx], 1, additionalCost, C);
                if (branch_1 is not null) // calculate estimates for branch+1
                {
                    List<double> reliabilities2 = new List<double>();
                    for (int i = 0; i < TRIALS; i++)
                    {
                        var node = CalculateRelibilityForConfig(myScheme, branch_1.Branch, numberOfNodes);
                        if (node is not null)
                        {
                            reliabilities2.Add(node.ReliabilityValue);
                        }
                    }
                    upperReliabilityEstim1 = reliabilities2.Average();

                    lowerReliabilityEstimate1 = CalculateRelibilityForConfig(myScheme, branch_1.Branch, numberOfNodes)!.ReliabilityValue;
                }

                Console.WriteLine($"{lowerReliabilityEstimate0}, {lowerReliabilityEstimate1}\t{upperReliabilityEstim0}, {upperReliabilityEstim1}");

                if (lowerReliabilityEstimate0 is not null && lowerReliabilityEstimate1 is not null
                    && upperReliabilityEstim0 is not null && upperReliabilityEstim1 is not null)
                {
                    if (Math.Max((double)lowerReliabilityEstimate0, (double)lowerReliabilityEstimate1) <=
                        Math.Min((double)upperReliabilityEstim0, (double)upperReliabilityEstim1))
                    {
                        branchList.Remove(parentNode);
                        branchList.Add(branch_0!);
                        branchList.Add(branch_1!);

                        // choose next parentNode
                        if (upperReliabilityEstim1 > upperReliabilityEstim0)
                        {
                            parentNode = branch_1;
                        }
                        else
                        {
                            parentNode = branch_0;
                        }

                        currentLevel++;
                    }
                }
                else
                    currentLevel--;
            }
        }       
    }
}