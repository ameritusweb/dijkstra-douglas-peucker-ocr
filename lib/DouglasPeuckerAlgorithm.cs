using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public class DouglasPeuckerAlgorithm
    {
        public List<Node> DouglasPeucker(List<Node> points, double angleTolerance)
        {
            return DouglasPeucker(points, 0, points.Count - 1, angleTolerance);
        }

        private List<Node> DouglasPeucker(List<Node> points, int startIndex, int endIndex, double angleTolerance)
        {
            if (startIndex >= endIndex)
            {
                return new List<Node>();
            }

            double maxAngleDifference = 0;
            int indexToSplit = startIndex;

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                double angle = CalculateAngle(points[startIndex], points[i], points[endIndex]);

                if (angle > maxAngleDifference)
                {
                    maxAngleDifference = angle;
                    indexToSplit = i;
                }
            }

            if (maxAngleDifference > angleTolerance)
            {
                var firstPart = DouglasPeucker(points, startIndex, indexToSplit, angleTolerance);
                var secondPart = DouglasPeucker(points, indexToSplit, endIndex, angleTolerance);

                var result = new List<Node>(firstPart);
                result.AddRange(secondPart.Skip(1)); // Avoid duplicate points
                return result;
            }
            else
            {
                return new List<Node> { points[startIndex], points[endIndex] };
            }
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
