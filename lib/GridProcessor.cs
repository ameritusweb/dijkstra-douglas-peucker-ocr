using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Concurrent;

namespace ImageProcess
{
    public class GridProcessor
    {
        private Node[,] grid;
        private int gridX, gridY;
        private readonly int angleTolerance = 30;

        public GridProcessor(Image<Gray, Byte> image)
        {
            this.gridX = image.Width;
            this.gridY = image.Height;
            grid = new Node[this.gridY, this.gridX];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    grid[y, x] = new Node(y, x, image.Data[y, x, 0] <= 128);
                }
            }
        }

        public IReadOnlyList<List<Node>> ProcessGrid()
        {
            var pointPairs = GenerateCornerPointPairs();
            DouglasPeuckerAlgorithm douglasPeuckerAlgorithm = new DouglasPeuckerAlgorithm();

            ConcurrentBag<List<Node>> lineSegmentBag = new ConcurrentBag<List<Node>>();

            var options = new ParallelOptions { MaxDegreeOfParallelism = 16 };

            // process in parallel
            Parallel.ForEach(pointPairs, options, pair =>
            {
                var (start, end) = pair;
                if (!start.IsForeground || !end.IsForeground)
                {
                    throw new InvalidOperationException("Start or end node is not foreground");
                }

                var dgrid = new Node[this.gridY, this.gridX];
                int total = 0;
                for (int y = 0; y < this.gridY; y++)
                {
                    for (int x = 0; x < this.gridX; x++)
                    {
                        dgrid[y, x] = this.grid[y, x].Clone();
                        if (dgrid[y, x].IsForeground)
                        {
                            total++;
                        }
                    }
                }

                DijkstrasAlgorithm dAlg = new DijkstrasAlgorithm(dgrid, dgrid.GetLength(1), dgrid.GetLength(0), total);
                var shortestPath = dAlg.FindShortestPath(dgrid[start.Point.Y, start.Point.X], dgrid[end.Point.Y, end.Point.X]); // finds the shortest path from start to end for foreground neighbor nodes
                var lineSegments = douglasPeuckerAlgorithm.DouglasPeucker(shortestPath.ToList(), angleTolerance); // finds the minimal # of line segments that represents the path
                lineSegmentBag.Add(lineSegments);
            });

            return lineSegmentBag.ToList().AsReadOnly();
        }

        public (Node topLeft, Node topRight, Node bottomLeft, Node bottomRight) FindCornerPoints()
        {
            Node topLeft = null, topRight = null, bottomLeft = null, bottomRight = null;
            double minTopLeftDistance = double.MaxValue, minTopRightDistance = double.MaxValue,
                   minBottomLeftDistance = double.MaxValue, minBottomRightDistance = double.MaxValue;

            for (int y = 0; y < gridY; y++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    Node currentNode = grid[y, x];

                    if (currentNode.IsForeground)
                    {
                        // Calculate distances to the corners
                        double distanceToTopLeft = Math.Sqrt(x * x + y * y);
                        double distanceToTopRight = Math.Sqrt((gridX - x - 1) * (gridX - x - 1) + y * y);
                        double distanceToBottomLeft = Math.Sqrt(x * x + (gridY - y - 1) * (gridY - y - 1));
                        double distanceToBottomRight = Math.Sqrt((gridX - x - 1) * (gridX - x - 1) + (gridY - y - 1) * (gridY - y - 1));

                        // Check if the current node is closer to the top left than the current best
                        if (distanceToTopLeft < minTopLeftDistance)
                        {
                            topLeft = currentNode;
                            minTopLeftDistance = distanceToTopLeft;
                        }

                        // Check if the current node is closer to the top right than the current best
                        if (distanceToTopRight < minTopRightDistance)
                        {
                            topRight = currentNode;
                            minTopRightDistance = distanceToTopRight;
                        }

                        // Check if the current node is closer to the bottom left than the current best
                        if (distanceToBottomLeft < minBottomLeftDistance)
                        {
                            bottomLeft = currentNode;
                            minBottomLeftDistance = distanceToBottomLeft;
                        }

                        // Check if the current node is closer to the bottom right than the current best
                        if (distanceToBottomRight < minBottomRightDistance)
                        {
                            bottomRight = currentNode;
                            minBottomRightDistance = distanceToBottomRight;
                        }
                    }
                }
            }

            return (topLeft, topRight, bottomLeft, bottomRight);
        }

        public List<(Node, Node)> GenerateCornerPointPairs()
        {
            var (topLeft, topRight, bottomLeft, bottomRight) = FindCornerPoints();

            var pairs = new List<(Node, Node)>
        {
            (topLeft, topRight),
            (topLeft, bottomLeft),
            (topLeft, bottomRight),
            (topRight, bottomLeft),
            (topRight, bottomRight),
            (bottomLeft, bottomRight)
        };

            // Filter out any pairs where either node is null (if a corner wasn't found)
            return pairs.Where(pair => pair.Item1 != null && pair.Item2 != null).ToList();
        }
    }
}
