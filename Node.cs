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
        public double ReliabilityExtra = 0;
        

        public double ReliabilityValue => hasExtra ? ReliabilityExtra : Reliability;
        
        public Node(long iD, double lambdaT, double lambdaTau, double cost)
        {
            ID = iD;
            LambdaT = lambdaT;
            LambdaTau = lambdaTau;
            Cost = cost;
        }

        public void CalculateNodeReliability(double randVal)
        {
            Reliability = LambdaT * Math.Log(1 / (1 - randVal));
            var timeToLiveExtra = LambdaTau * Math.Log(1 / (1 - randVal));
            ReliabilityExtra = Reliability + timeToLiveExtra;
        }

    }
}