using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace ImageProcess
{
    public class AxisAnalyzer
    {

        public AxisAnalyzer()
        {

        }

        public (double maxAxis, double minAxis, double angle) Calculate(Node[,]  nodes)
        {
            List<Node> section = new List<Node>();

            for (int i = 0; i < nodes.GetLength(0); i++)
            {
                for (int j = 0; j < nodes.GetLength(1); j++)
                {
                    if (nodes[i, j].IsForeground)
                    {
                        section.Add(nodes[i, j]);
                    }
                }
            }

            // Calculate image moments
            var (m00, m10, m01, m20, m02, m11) = CalculateMoments(section);

            // Calculate centroid
            var centroidM = CalculateCentroid(m00, m10, m01);

            // Calculate covariance matrix
            var (covXX, covYY, covXY) = CalculateCovarianceMatrix(m00, m20, m02, m11, centroidM);

            // Find principal axis
            var (majorAxisLength, minorAxisLength, angle) = FindPrincipalAxis(covXX, covYY, covXY);

            return (majorAxisLength, minorAxisLength, angle);
        }

        private (double m00, double m10, double m01, double m20, double m02, double m11) CalculateMoments(List<Node> section)
        {
            double m00 = 0, m10 = 0, m01 = 0, m20 = 0, m02 = 0, m11 = 0;
            foreach (var node in section)
            {
                m00 += 1;
                m10 += node.X;
                m01 += node.Y;
                m20 += node.X * node.X;
                m02 += node.Y * node.Y;
                m11 += node.X * node.Y;
            }

            return (m00, m10, m01, m20, m02, m11);
        }

        private (double x, double y) CalculateCentroid(double m00, double m10, double m01)
        {
            return (m10 / m00, m01 / m00);
        }

        private (double covXX, double covYY, double covXY) CalculateCovarianceMatrix(double m00, double m20, double m02, double m11, (double x, double y) centroid)
        {
            double covXX = m20 / m00 - centroid.x * centroid.x;
            double covYY = m02 / m00 - centroid.y * centroid.y;
            double covXY = m11 / m00 - centroid.x * centroid.y;

            return (covXX, covYY, covXY);
        }

        private (double majorAxisLength, double minorAxisLength, double angle) FindPrincipalAxis(double covXX, double covYY, double covXY)
        {
            double theta = 0.5 * Math.Atan2(2 * covXY, covXX - covYY);
            double eigVal1 = 0.5 * (covXX + covYY) + 0.5 * Math.Sqrt(4 * covXY * covXY + (covXX - covYY) * (covXX - covYY));
            double eigVal2 = 0.5 * (covXX + covYY) - 0.5 * Math.Sqrt(4 * covXY * covXY + (covXX - covYY) * (covXX - covYY));

            double majorAxisLength = Math.Sqrt(eigVal1);
            double minorAxisLength = Math.Sqrt(eigVal2);

            return (majorAxisLength, minorAxisLength, theta);
        }
    }
}
