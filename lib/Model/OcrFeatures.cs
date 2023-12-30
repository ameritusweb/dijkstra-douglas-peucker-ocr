using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess.Model
{
    public class OcrFeatures
    {
        // General features
        public BoundingBox BoundingBox { get; set; }
        public double AspectRatio { get; set; }
        public double CentroidX { get; set; }
        public double CentroidY { get; set; }
        public double MassToTotalArea { get; set; }

        // Intersection analysis
        public List<List<int>> IntersectionArrays { get; set; }

        // Negative spaces
        public int NumberOfNegativeSpaces { get; set; }
        public List<SectionMetrics> NegativeSpaces { get; set; }

        // Negative space borders
        public int NumberOfNegativeSpaceBorders { get; set; }
        public List<SectionMetrics> NegativeSpaceBorders { get; set; }

        // Longest shortest path inside positive space
        public ShortestPath LongestShortestPath { get; set; }
    }
}
