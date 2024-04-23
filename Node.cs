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
            var randT = new Random();
            var randTau = new Random();
            double TimeToLive = LambdaT * Math.Log(1 / (1 - randT.NextDouble()));
            double TimeToLiveExtra = xij * LambdaTau * Math.Log(1 / (1 - randTau.NextDouble()));
            return TimeToLive + TimeToLiveExtra;
        }
    }
}