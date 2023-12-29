using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public static class NodeExtensions
    {
        public static List<Point> GetForegroundPoints(this Node[,] nodes)
        {
            var points = new List<Point>();
            int rows = nodes.GetLength(0);
            int columns = nodes.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (nodes[i, j].IsForeground)
                    {
                        points.Add(nodes[i, j].Point);
                    }
                }
            }

            return points;
        }
    }
}
