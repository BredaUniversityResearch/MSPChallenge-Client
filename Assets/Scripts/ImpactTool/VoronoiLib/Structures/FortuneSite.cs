using System.Collections.Generic;

namespace VoronoiLib.Structures
{
	public class FortuneSite
	{
		public string Name { get; }
		public double X { get; }
        public double Y { get; }

        public List<VEdge> Cell { get; private set; }

        public List<FortuneSite> Neighbors { get; private set; }

        public FortuneSite(string name, double x, double y)
        {
            Name = name;
            X = x;
            Y = y;
            Cell = new List<VEdge>();
            Neighbors = new List<FortuneSite>();
        }
    }
}
