using AppLogic;
using System.Numerics;

internal class Program
{
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

    static Stack<Dictionary<int, ReservedNode>> checkBranch(Scheme? myScheme, Stack<Dictionary<int, ReservedNode>> branchStack,
        double C, int numberOfNodes, List<int> fixedBits)
    {
        int idx = 0;
        const int TRIALS = 1000;
        double lowerReliabilityEstimate1 = 0, lowerReliabilityEstimate2 = 0;
        double upperReliabilityEstim1 = 1, upperReliabilityEstim2 = 1;
        double fixedBudget = 0;
        Console.WriteLine(idx);

        while (C >= fixedBudget && idx < numberOfNodes && branchStack.Count > 0)
        {
            idx++;
            // printStack(branchStack);
            // pop current brunch and push 2 new branches
            var baseBrunch = branchStack.Pop();

            var branch_1 = createBranch(baseBrunch, fixedBits[idx], 0, 0, C);
            if (branch_1 is not null)
            {
                branchStack.Push(branch_1);

                lowerReliabilityEstimate1 = CalculateRelibilityForConfig(myScheme!, branch_1, numberOfNodes)!.Reliability;

                List<double> reliabilities = new List<double>();
                for (int i = 0; i < TRIALS; i++)
                {
                    var node = CalculateRelibilityForConfig(myScheme!, branch_1, numberOfNodes);
                    if (node is not null)
                    {
                        reliabilities.Add(node.Reliability);
                    }
                }

                upperReliabilityEstim1 = reliabilities.Average();
                //branchStack.Push(branch_1);
            }

            //Console.WriteLine(idx);
            var additionalCost = myScheme!.scheme.Values.SelectMany(x => x).Where(x => x.ID == idx + 1).First().Cost;

            var branch_2 = createBranch(baseBrunch, fixedBits[idx], 1, additionalCost, C);
            if (branch_2 is not null)
            {
                branchStack.Push(branch_2);
                List<double> reliabilities2 = new List<double>();
                for (int i = 0; i < TRIALS; i++)
                {
                    var node = CalculateRelibilityForConfig(myScheme, branch_2, numberOfNodes);
                    if (node is not null)
                    {
                        reliabilities2.Add(node.Reliability);
                    }
                }
                upperReliabilityEstim2 = reliabilities2.Average();

                lowerReliabilityEstimate2 = CalculateRelibilityForConfig(myScheme, branch_2, numberOfNodes)!.Reliability;


                // choose to push branch
                if (Math.Max(lowerReliabilityEstimate1, lowerReliabilityEstimate2) <=
                   Math.Min(upperReliabilityEstim1, upperReliabilityEstim2))
                {
                    fixedBudget += additionalCost;
                    branchStack.Push(branch_2);
                }

            }
        }
        return branchStack;
    }

    static void printStack(Stack<Dictionary<int, ReservedNode>> stack)
    {
        while (stack.Count > 0)
        {
            var branch = stack.Pop();
            foreach (var elem in branch)
            {
                Console.WriteLine($"{elem.Key}\t | {elem.Value.Bit} -> {elem.Value.Budget}");
            }
        }
    }

    private static void Main(string[] args)
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

        int idx = 0;

        var additionalCost = myScheme.scheme.Values.SelectMany(x => x).Where(x => x.ID == idx + 1).First().Cost;
        var branch1 = createBranch(new Dictionary<int, ReservedNode>(), fixedBits[idx], 0, 0, C);
        var branch2 = createBranch(new Dictionary<int, ReservedNode>(), fixedBits[idx], 1, additionalCost, C); //new Dictionary<int, ReservedNode>();


        Stack<Dictionary<int, ReservedNode>> branchStack = new Stack<Dictionary<int, ReservedNode>>();


        branchStack.Push(branch1!);
        if (branch1 is not null)
        {
            branchStack = checkBranch(myScheme, branchStack, C, numberOfNodes, fixedBits);
            printStack(branchStack);
        }

        branchStack.Clear();
        branchStack.Push(branch2!);
        if (branch2 is not null)
        {
            branchStack = checkBranch(myScheme, branchStack, C, numberOfNodes, fixedBits);
            printStack(branchStack);
        }
    }
}