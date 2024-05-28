using AppLogic;
using Newtonsoft.Json;

internal partial class Program
{
    
    static Node? CalculateRelibilityForConfig(Scheme scheme, Dictionary<Key, ReservedNode> branch, int numberOfNodes)
    {
        int count = 0;
        while (true)
        {
            count++;
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
                        node.hasExtra = bit == '1';
                        return node.ReliabilityValue;
                    })!); // min for serial part
                }

                // calculate max chain reliability for scheme
                var schemeReliability = minReliableNodesInChains.MaxBy(x => x.ReliabilityValue)!; // max for parallel part

                if (count >= 100000)
                {
                    Console.WriteLine($"Count = {count}\tConfig = {binaryConfig}; \t");
                    break;
                }
                    
                return schemeReliability;
            }
        }
        return null;
    }

    static LeafNode? createBranch(LeafNode parentBranch, int index, int bit, double cost, double C)
    {
        if (parentBranch.TotalCost + cost <= C)
        {
            var updatedBranch = new LeafNode(parentBranch.Branch, parentBranch.Level + 1, parentBranch.TotalCost + cost);
            updatedBranch.Branch[new Key(index, bit)] = new ReservedNode()
            {
                Bit = bit,
                Budget = cost
            };

            return updatedBranch;
        }
        return null;
    }

    static int[] GenerateRandomConfigWithFixedBits(Dictionary<Key, ReservedNode> fixedBits, int numberOfBits)
    {
        int[] res = new int[numberOfBits];

        var fixedIdxs = fixedBits.Keys.Select(x => x.Id);

        
        var indexes = new HashSet<int>(Enumerable.Range(0, numberOfBits)).Except(fixedIdxs);
        var r = new Random();

        foreach (var bit in fixedBits) {
            if(bit.Key.Id != -1)
                res[bit.Key.Id] = bit.Value.Bit;
        }

        foreach (int bitIndex in indexes)
        {
            res[bitIndex] = r.Next(0, 2);
        }

        //Console.WriteLine(string.Join("", res));

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

        double C = 220.0;      // all = 550      
        var myScheme = new Scheme(scheme, C);

        
        int numberOfNodes = myScheme.scheme.Values.Sum(list => list.Count);

        var r = new Random();

        foreach (var chain in myScheme.scheme.Values)
            chain.AsParallel().ForAll(node =>
            {
                var randVal = r.NextDouble();
                node.CalculateNodeReliability(randVal);
            });

        var json = JsonConvert.SerializeObject(myScheme);

        var fixedBits = Enumerable.Range(0, numberOfNodes)
                                        .Take(numberOfNodes)
                                        .OrderBy(i => Guid.NewGuid())
                                        .ToList();

        
        fixedBits.ForEach(x => Console.Write($"{x}  "));// (fixedBits.ToString());
        List<LeafNode> branchList = new List<LeafNode>();

        Console.WriteLine();

        /*
        // adds fictitious node to start iterations
        var fictBranch = new Dictionary<Key, ReservedNode>() {{
            new Key(-1, 0), new ReservedNode()
            {
                Bit = -1,
                Budget = 0
            }
        } };
        branchList.Add(new LeafNode(fictBranch, 0, 0));*/
        int currentLevel = 0;
        const int TRIALS = 1000;
        
        LeafNode? parentNode = null;
        //var parentNode = branchList.Last();

        while (currentLevel < numberOfNodes)
        {
            if (branchList.Count != 0)
                branchList.Remove(parentNode!);
            else
            {
                parentNode = new LeafNode(new Dictionary<Key, ReservedNode>(), 0, 0);
            }
                
            double? lowerReliabilityEstimate0 = null, lowerReliabilityEstimate1 = null;
            double? upperReliabilityEstim0 = null, upperReliabilityEstim1 = null;

            // create branch with next 0
            var branch_0 = createBranch(parentNode!, fixedBits[currentLevel], 0, 0, C);
            lowerReliabilityEstimate0 = CalculateRelibilityForConfig(myScheme!, branch_0!.Branch, numberOfNodes)!.ReliabilityValue;

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

            Console.WriteLine("branch0 was created");

            // try to create branch with next 1
            var additionalCost = myScheme!.scheme.Values.SelectMany(x => x).Where(x => x.ID == fixedBits[currentLevel] + 1).First().Cost;

            var branch_1 = createBranch(parentNode!, fixedBits[currentLevel], 1, additionalCost, C);
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
           
                Console.WriteLine("branch1 was created");
            }
            else
            {
                Console.WriteLine("branch1 is null");
            }

            Console.WriteLine($"{lowerReliabilityEstimate0}, {lowerReliabilityEstimate1}\t{upperReliabilityEstim0}, {upperReliabilityEstim1}");

            if ( //lowerReliabilityEstimate0 is not null &&
                upperReliabilityEstim1 is not null                
                && upperReliabilityEstim0 is not null
                //&& lowerReliabilityEstimate1 is not null
                )
            {
                // choose next parentNode (branch0 or branch1)
                if (upperReliabilityEstim1 >= upperReliabilityEstim0)
                {
                    parentNode = branch_1!;
                    branchList.Add(branch_1!);
                }
                else
                {
                    parentNode = branch_0;
                    branchList.Add(branch_0);
                }
             
                parentNode.Level = currentLevel+1;

                Console.WriteLine($"Parent node: {parentNode}");
            }

            if (lowerReliabilityEstimate0 is null || 
                lowerReliabilityEstimate1 is null)
            {
                break;
            }
            //Console.WriteLine($"Current node = {fixedBits[currentLevel]}, Cost = {additionalCost}");

            currentLevel += 1;
            Console.WriteLine(currentLevel);
        }

        /*
        foreach (var elem in branchList)
            Console.WriteLine(elem.ToString());
        */
    }
}