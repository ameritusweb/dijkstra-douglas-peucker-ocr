using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public class SectionMetrics
    {
        public double Circularity { get; set; }
        public double AspectRatio { get; set; }
        public double CentroidPositionX { get; set; }

        public double CentroidPositionY { get; set; }
        public double RelativeMass { get; set; }
        public double RelativeMax { get; set; }
        public double MajorAxisLength { get; set; }
        public double MinorAxisLength { get; set; }
        public double AxisAngle { get; set; }

        public bool IsEnclosed { get; set; }
    }
}
