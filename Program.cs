using AppLogic;
using System.Numerics;

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

static int[] GenerateRandomConfigWithFixedBits(Dictionary<int, ReservedNode> fixedBits, int numberOfBits)
{
    int[] res = new int[numberOfBits];
    var indexes = new HashSet<int>( Enumerable.Range(0, numberOfBits) ).Except(fixedBits.Keys);
    var r = new Random();
    
    foreach(int bitIndex in indexes)
    {
        res[bitIndex] = r.Next(0, 2);
    }

    return res;
}

static Node? CalculateRelibilityForConfig(Scheme scheme, Dictionary<int, ReservedNode> branch, int numberOfNodes)
{
    while (true)
    {
        string binaryConfig = string.Join("", GenerateRandomConfigWithFixedBits(branch, numberOfNodes)); // GetBinaryConfig(config, numberOfNodes);

        if (scheme.isGeneratedSchemeFromConfigUnderBudget(binaryConfig))
        {
            // calculate min reliabilities for chains
            List<Node> minReliableNodesInChains = new List<Node>();

            foreach (var chain in scheme.scheme.Values)
            {
                chain.AsParallel().ForAll(node => node.Reliability = node.CalculateNodeReliability(node.hasExtra ? 1 : 0));

                minReliableNodesInChains.Add(chain.MinBy(node => node.Reliability)!); // min for serial part
            }

            // calculate max chain reliability for scheme
            var schemeReliability = minReliableNodesInChains.MaxBy(x => x.Reliability)!; // max for parallel part

            return schemeReliability;
        }
    }
}

double C = 200.0;      // all = 550      
var myScheme = new Scheme(scheme, C);
int numberOfNodes = myScheme.scheme.Values.Sum(list => list.Count);
const int TRIALS = 1000;

var r = new Random();
var fixedBits = Enumerable.Range(0, numberOfNodes)
                                .Take(numberOfNodes)
                                .OrderBy(i => Guid.NewGuid())
                                .ToList();

int idx = 0;
double fixedBudget = 0;

var branch1 = new Dictionary<int, ReservedNode>();
var branch2 = new Dictionary<int, ReservedNode>();

while (C > fixedBudget && idx < numberOfNodes)
{
    Console.WriteLine(idx);
    // get random bit and create 2 branches
    var currentBudget = branch1.Where(x => x.Value.Bit == 1).Sum(x => x.Value.Budget);

    branch1[fixedBits[idx]] = new ReservedNode()  {
        Bit = 0, Budget = 0
    };
    
    // calculate upper estimate
    List<double> reliabilities = new List<double>();
    for (int i = 0; i < TRIALS; i++)
    {
        var node = CalculateRelibilityForConfig(myScheme, branch1, numberOfNodes);
        if (node is not null)
        {
            reliabilities.Add(node.Reliability);
        }
    }

    var upperReliabilityEstim1 = reliabilities.Average();

    var additionalCost = myScheme.scheme.Values.SelectMany(x => x).Where(x => x.ID == idx + 1).First().Cost;

    if (currentBudget + additionalCost < C)
    {
        branch2[fixedBits[idx]] = new ReservedNode()
        {
            Bit = 1,
            Budget = additionalCost
        };
    }
    else { idx++; continue; }

    List<double> reliabilities2 = new List<double>();
    for (int i = 0; i < TRIALS; i++)
    {
        var node = CalculateRelibilityForConfig(myScheme, branch2, numberOfNodes);
        if (node is not null)
        {
            reliabilities2.Add(node.Reliability);
        }
    }
    var upperReliabilityEstim2 = reliabilities2.Average();

    // calculate lower estimate
    var nodeLowerEstimate1 = CalculateRelibilityForConfig(myScheme, branch1, numberOfNodes);
    var nodeLowerEstimate2 = CalculateRelibilityForConfig(myScheme, branch2, numberOfNodes);

    // choose branch
    if (nodeLowerEstimate1 is not null && nodeLowerEstimate2 is not null)
    {
        if (Math.Max(nodeLowerEstimate1.Reliability, nodeLowerEstimate2.Reliability) < Math.Min(upperReliabilityEstim1, upperReliabilityEstim2))
        {                       
            if (upperReliabilityEstim2 > upperReliabilityEstim1)
            {
                branch1 = branch2;
                fixedBudget = currentBudget + additionalCost;
            }
            else
            {
                branch2 = branch1;
                fixedBudget = currentBudget;
            }            
        }
    }
    idx++;
}

int[] resultConfig = new int[numberOfNodes];
foreach (var elem in branch1)
{
    Console.WriteLine($"{elem.Key}\t | {elem.Value.Bit} -> {elem.Value.Budget}");
    resultConfig[elem.Key] = elem.Value.Bit;
}

string binaryResultConfig = string.Join("", resultConfig);
var resultBudget = branch1.Where(x => x.Value.Bit == 1).Sum(x => x.Value.Budget);
Console.WriteLine($"Result Budget = {resultBudget}");
Console.WriteLine($"Result Config = {binaryResultConfig}");
// calculate result reliability

var resultNode = CalculateRelibilityForConfig(myScheme, branch1, numberOfNodes);
Console.WriteLine(resultNode?.Reliability);