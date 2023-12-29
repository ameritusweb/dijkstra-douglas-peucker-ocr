using Emgu.CV;
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
                var (maxDist, minDist) = FindMaxMinDistances(section, centroid);
                sectionMetrics.AspectRatio = maxDist / minDist;

                metrics.Add(sectionMetrics);
            }
            return metrics.AsReadOnly();
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

        private Node CalculateCentroid(List<Node> section)
        {
            double xSum = 0, ySum = 0;
            foreach (var node in section)
            {
                xSum += node.X;
                ySum += node.Y;
            }
            return new Node((int)(ySum / section.Count), (int)(xSum / section.Count), false);
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
    }
}
