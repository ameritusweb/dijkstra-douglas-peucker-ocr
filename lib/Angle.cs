using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public class Angle
    {

        public Angle(int angle, List<int> intersections)
        {
            this.angle = angle;
            this.intersections = intersections;
        }

        public int angle { get; set; }

        public List<int> intersections { get; set; }
    }
}
