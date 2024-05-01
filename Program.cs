using AppLogic;
using System;
using System.Numerics;

internal class Program
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
                    //chain.AsParallel().ForAll(node => node.Reliability = node.CalculateNodeReliability(node.hasExtra ? 1 : 0));

                    minReliableNodesInChains.Add(chain.MinBy(node => node.Reliability)!); // min for serial part
                }

                // calculate max chain reliability for scheme
                var schemeReliability = minReliableNodesInChains.MaxBy(x => x.Reliability)!; // max for parallel part

                return schemeReliability;
            }
        }
    }

    static List<Dictionary<int, ReservedNode>> checkBranch(Scheme? myScheme, List<Dictionary<int, ReservedNode>?> branchList,
        double C, int numberOfNodes, List<int> fixedBits, int currentNodeIndex, int currentIndex)
    {
        const int TRIALS = 1000;
        double lowerReliabilityEstimate1 = 0, lowerReliabilityEstimate2 = 0;
        double upperReliabilityEstim1 = 100, upperReliabilityEstim2 = 100;

        // push 2 new branches
        if (branchList[currentIndex] is not null)
        {
            var branch_0 = createBranch(branchList[currentIndex]!, fixedBits[currentNodeIndex-1], 0, 0, C);
            if (branch_0 is not null)
            {
                lowerReliabilityEstimate1 = CalculateRelibilityForConfig(myScheme!, branch_0, numberOfNodes)!.Reliability;

                List<double> reliabilities = new List<double>();
                for (int i = 0; i < TRIALS; i++)
                {
                    var node = CalculateRelibilityForConfig(myScheme!, branch_0, numberOfNodes);
                    if (node is not null)
                    {
                        reliabilities.Add(node.Reliability);
                    }
                }

                upperReliabilityEstim1 = reliabilities.Average();
            }

            //Console.WriteLine(idx);
            var additionalCost = myScheme!.scheme.Values.SelectMany(x => x).Where(x => x.ID == currentNodeIndex).First().Cost;

            var branch_1 = createBranch(branchList[currentIndex]!, fixedBits[currentNodeIndex-1], 1, additionalCost, C);
            if (branch_1 is not null)
            {
                List<double> reliabilities2 = new List<double>();
                for (int i = 0; i < TRIALS; i++)
                {
                    var node = CalculateRelibilityForConfig(myScheme, branch_1, numberOfNodes);
                    if (node is not null)
                    {
                        reliabilities2.Add(node.Reliability);
                    }
                }
                upperReliabilityEstim2 = reliabilities2.Average();

                lowerReliabilityEstimate2 = CalculateRelibilityForConfig(myScheme, branch_1, numberOfNodes)!.Reliability;


                // choose to push branch
                if (Math.Max(lowerReliabilityEstimate1, lowerReliabilityEstimate2) <=
                    Math.Min(upperReliabilityEstim1, upperReliabilityEstim2))
                {
                    branchList[2 * currentIndex + 1] = branch_0;
                    branchList[2 * currentIndex + 2] = branch_1;
                    //branchList[currentIndex] = null;
                }
                else
                {
                    branchList[2 * currentIndex + 1] = null;
                    branchList[2 * currentIndex + 2] = null;
                }

                
            }
        }
        else
        {
            branchList[2 * currentIndex + 1] = null;
            branchList[2 * currentIndex + 2] = null;
            //branchList[currentIndex] = null;
        }
 
        return branchList!;
    }

    static Dictionary<int, ReservedNode>? createBranch(Dictionary<int, ReservedNode> branch, int index, int bit, double cost, double C)
    {
        var currentBudget = branch.Where(x => x.Value.Bit == 1).Sum(x => x.Value.Budget);
        if (currentBudget + cost < C)
        {
            branch[index] = new ReservedNode()
            {
                Bit = bit,
                Budget = cost
            };

            return branch;
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

    static void printList(List<Dictionary<int, ReservedNode>> list)
    {
        Console.WriteLine("#########################################################");
        foreach (var branch in list)
        {
            if (branch != null)
            {
                foreach (var elem in branch)
                {
                    Console.WriteLine($"{elem.Key}\t | {elem.Value.Bit} -> {elem.Value.Budget}");
                }

                Console.WriteLine();
            }

            
        }
        Console.WriteLine("#########################################################");
    }

    private const string ResDir = "./level-logs/";
    static void writeList(List<Dictionary<int, ReservedNode>> list, int level)
    {
        var logFilePath = Path.Combine(ResDir, $"level-{level}.txt");

        var texts = new List<string>();
        
        foreach (var branch in list)
        {
            if (branch != null)
            {
                foreach (var elem in branch)
                {
                    texts.Add($"{elem.Key}\t | {elem.Value.Bit} -> {elem.Value.Budget}");
                }

                texts.Add($"");
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


        foreach (var chain in myScheme.scheme.Values)
            chain.AsParallel().ForAll(node => node.Reliability = node.CalculateNodeReliability(node.hasExtra ? 1 : 0));

        var r = new Random();
        var fixedBits = Enumerable.Range(0, numberOfNodes)
                                        .Take(numberOfNodes)
                                        .OrderBy(i => Guid.NewGuid())
                                        .ToList();
        int currentNodeIndex = 1;
        var additionalCost = myScheme.scheme.Values.SelectMany(x => x).Where(x => x.ID == currentNodeIndex).First().Cost;


        List<Dictionary<int, ReservedNode>> branchList = new List<Dictionary<int, ReservedNode>>(new Dictionary<int, ReservedNode>[(long)(Math.Pow(2, numberOfNodes+1)+1)]);

        // level 0
        branchList[0] = new Dictionary<int, ReservedNode>() 
        {
            {
                0, new ReservedNode() { Bit = -1, Budget = 0 }
            } 
        }; 

        // level 1
        branchList[1] = createBranch(new Dictionary<int, ReservedNode>(), fixedBits[currentNodeIndex - 1], 0, 0, C)!;
        branchList[2] = createBranch(new Dictionary<int, ReservedNode>(), fixedBits[currentNodeIndex - 1], 1, additionalCost, C)!;

        // level 1..numberOfNodes
        for(currentNodeIndex = 1; currentNodeIndex < numberOfNodes; currentNodeIndex++)
        {
            Console.WriteLine(currentNodeIndex);
            for (int h = 0; h < Math.Pow(2, currentNodeIndex); h++)
            {
                var currentChildIndex = 2 * (currentNodeIndex - 1) + 1 + h;
                //Console.WriteLine($"Child Index = {currentChildIndex}");
                branchList = checkBranch(myScheme, branchList!, C, numberOfNodes, fixedBits, currentNodeIndex, currentChildIndex);
            }
            //printList(branchList);
            writeList(branchList, currentNodeIndex);
        }     
    }
}