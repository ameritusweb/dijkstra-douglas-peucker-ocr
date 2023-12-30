using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public class LineSegmentProcessor
    {
        public double CalculateTotalLength(List<Node> nodes)
        {
            double totalLength = 0.0;
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                totalLength += DistanceBetween(nodes[i], nodes[i + 1]);
            }
            return totalLength;
        }

        public double CalculateAspectRatio(List<Node> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("The list of nodes is null or empty.");
            }

            int minX = nodes.Min(node => node.X);
            int maxX = nodes.Max(node => node.X);
            int minY = nodes.Min(node => node.Y);
            int maxY = nodes.Max(node => node.Y);

            int width = maxX - minX;
            int height = maxY - minY;

            if (height == 0)
            {
                throw new InvalidOperationException("Height cannot be zero for aspect ratio calculation.");
            }

            return (double)width / height;
        }

        private double DistanceBetween(Node a, Node b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }

        public List<double> CalculateAngles(List<Node> nodes)
        {
            var angles = new List<double>();
            for (int i = 1; i < nodes.Count - 1; i++)
            {
                angles.Add(CalculateAngle(nodes[i - 1], nodes[i], nodes[i + 1]));
            }
            return angles;
        }

        private double CalculateAngle(Node a, Node b, Node c)
        {
            // Vector AB
            double abX = b.X - a.X;
            double abY = b.Y - a.Y;

            // Vector BC
            double bcX = c.X - b.X;
            double bcY = c.Y - b.Y;

            // Dot product of AB and BC
            double dotProduct = (abX * bcX) + (abY * bcY);

            // Magnitudes of AB and BC
            double magnitudeAB = Math.Sqrt(abX * abX + abY * abY);
            double magnitudeBC = Math.Sqrt(bcX * bcX + bcY * bcY);

            // Calculate the cosine of the angle
            double cosAngle = dotProduct / (magnitudeAB * magnitudeBC);

            // Handling numerical inaccuracies that might lead cosAngle to slightly go out of bounds [-1, 1]
            cosAngle = Math.Max(-1, Math.Min(1, cosAngle));

            // Calculate the angle in radians and then convert to degrees
            double angle = Math.Acos(cosAngle) * (180.0 / Math.PI);

            return angle;
        }
    }
}
