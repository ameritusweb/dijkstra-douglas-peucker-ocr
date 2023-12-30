using Emgu.CV;
using Emgu.CV.ImgHash;
using Emgu.CV.Structure;

namespace ImageProcess
{
    public class ImageAnalyzer
    {
        private Image<Gray, Byte> image;
        private bool[,] visited;
        private List<List<Node>> sections = new List<List<Node>>();

        public ImageAnalyzer(Image<Gray, Byte> image)
        {
            this.image = image;
            visited = new bool[image.Height, image.Width];
        }

        public void Reset()
        {
            visited = new bool[image.Height, image.Width];
            sections = new List<List<Node>>();
        }

        public IReadOnlyList<SectionMetrics> CalculateMetrics(double intensity)
        {
            var metrics = new List<SectionMetrics>();
            foreach (var section in CountGraySections(intensity))
            {
                var sectionMetrics = new SectionMetrics();

                // Calculate Circularity
                int area = section.Count;
                int perimeter = CalculatePerimeter(section);
                sectionMetrics.Circularity = (4 * Math.PI * area) / Math.Pow(perimeter, 2);

                // Calculate Aspect Ratio
                var centroid = CalculateCentroid(section);
                var (maxDist, minDist) = FindMaxMinDistances(section, centroid.node);
                sectionMetrics.AspectRatio = maxDist / Math.Max(minDist, 1);
                sectionMetrics.CentroidPositionX = centroid.X;
                sectionMetrics.CentroidPositionY = centroid.Y;
                sectionMetrics.RelativeMass = area * 1d / (image.Width * image.Height) * 100d;
                sectionMetrics.RelativeMax = maxDist * 1d / Math.Sqrt(Math.Pow(image.Width, 2) + Math.Pow(image.Height, 2)) * 100d;

                if (sectionMetrics.RelativeMass > 0.5d)
                {
                    // Calculate image moments
                    var (m00, m10, m01, m20, m02, m11) = CalculateMoments(section);

                    // Calculate centroid
                    var centroidM = CalculateCentroid(m00, m10, m01);

                    // Calculate covariance matrix
                    var (covXX, covYY, covXY) = CalculateCovarianceMatrix(m00, m20, m02, m11, centroidM);

                    // Find principal axis
                    var (majorAxisLength, minorAxisLength, angle) = FindPrincipalAxis(covXX, covYY, covXY);
                    sectionMetrics.MajorAxisLength = majorAxisLength;
                    sectionMetrics.MinorAxisLength = minorAxisLength;
                    sectionMetrics.AxisAngle = angle;

                    sectionMetrics.IsEnclosed = !HasNeighbor(section, 220d);

                    metrics.Add(sectionMetrics);
                }
            }
            return metrics.AsReadOnly();
        }

        public bool HasNeighbor(List<Node> nodes, double intensity)
        {
            bool hasNeighbor = false;
            foreach (var node in nodes)
            {
                if (image[node.Y + 1, node.X].Intensity == intensity)
                {
                    hasNeighbor = true;
                    break;
                } else if (image[node.Y - 1, node.X].Intensity == intensity)
                {
                    hasNeighbor = true;
                    break;
                } else if (image[node.Y, node.X + 1].Intensity == intensity)
                {
                    hasNeighbor = true;
                    break;
                } else if (image[node.Y, node.X - 1].Intensity == intensity)
                {
                    hasNeighbor = true;
                    break;
                }
            }
            return hasNeighbor;
        }

        private int CalculatePerimeter(List<Node> section)
        {
            int perimeter = 0;
            foreach (var node in section)
            {
                if (IsEdgeNode(node, section))
                {
                    perimeter++;
                }
            }
            return perimeter;
        }

        private bool IsEdgeNode(Node node, List<Node> section)
        {
            int[][] directions = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
            foreach (var dir in directions)
            {
                int neighborX = node.X + dir[0];
                int neighborY = node.Y + dir[1];
                if (!section.Any(n => n.X == neighborX && n.Y == neighborY))
                {
                    return true;
                }
            }
            return false;
        }

        private (Node node, double X, double Y)  CalculateCentroid(List<Node> section)
        {
            double xSum = 0, ySum = 0;
            foreach (var node in section)
            {
                xSum += node.X;
                ySum += node.Y;
            }
            int centerX = (int)(xSum / section.Count);
            int centerY = (int)(ySum / section.Count);

            return (new Node((int)(ySum / section.Count), (int)(xSum / section.Count), false), MapCoordinate(centerX, image.Width),
                            MapCoordinate(centerY, image.Height));
        }

        private double MapCoordinate(int coordinate, int maxDimension)
        {
            // Maps the coordinate to the range of -10 to 10
            double relativePosition = 2.0 * (coordinate - (maxDimension / 2.0)) / maxDimension;
            return relativePosition * 10d;
        }

        private (double maxDist, double minDist) FindMaxMinDistances(List<Node> section, Node centroid)
        {
            double maxDist = 0;
            double minDist = double.MaxValue;
            foreach (var node in section.Where(n => IsEdgeNode(n, section)))
            {
                double dist = Math.Sqrt(Math.Pow(node.X - centroid.X, 2) + Math.Pow(node.Y - centroid.Y, 2));
                maxDist = Math.Max(maxDist, dist);
                minDist = Math.Min(minDist, dist);
            }
            return (maxDist, minDist);
        }

        public IReadOnlyList<List<Node>> CountGraySections(double intensity)
        {
            for (int y = 0; y < this.image.Height; y++)
            {
                for (int x = 0; x < this.image.Width; x++)
                {
                    if (image[y, x].Intensity == intensity && !visited[y, x])
                    {
                        var section = new List<Node>();
                        FloodFill(x, y, intensity, section);
                        sections.Add(section);
                    }
                }
            }
            return sections.AsReadOnly();
        }

        private void FloodFill(int x, int y, double intensity, List<Node> section)
        {
            if (x < 0 || y < 0 || x >= this.image.Width || y >= this.image.Height) return;
            if (visited[y, x] || image[y, x].Intensity != intensity) return;

            visited[y, x] = true;
            section.Add(new Node(y, x, false));

            FloodFill(x + 1, y, intensity, section);
            FloodFill(x - 1, y, intensity, section);
            FloodFill(x, y + 1, intensity, section);
            FloodFill(x, y - 1, intensity, section);
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
