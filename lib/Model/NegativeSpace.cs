using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess.Model
{
    public class NegativeSpace
    {
        public double Circularity { get; set; }
        public double MaximumAspectRatio { get; set; }
        public double CentroidX { get; set; }
        public double CentroidY { get; set; }
        public double MassToTotalArea { get; set; }
        // You can add more properties if needed
    }
}
