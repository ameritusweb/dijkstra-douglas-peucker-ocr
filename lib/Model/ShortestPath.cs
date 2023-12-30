using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess.Model
{
    public class ShortestPath
    {
        public double TotalLengthToDiagonalLengthRatio { get; set; }
        public int TotalNumberOfLineSegments { get; set; }
        public List<double> AngleChanges { get; set; }
        public string StartPosition { get; set; } // NW, SW, NE, SE
        public string EndPosition { get; set; } // NW, SW, NE, SE
        public double AspectRatio { get; internal set; }
    }
}
