using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public class Group
    {
        public Rectangle BoundingBox { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
