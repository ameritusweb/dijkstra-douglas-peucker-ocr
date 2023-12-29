namespace ImageProcess
{
    public class DijkstrasAlgorithm
    {
        private readonly int gridX;
        private readonly int gridY;
        private Node[,] grid;
        private int total;

        public int GridX => gridX;
        public int GridY => gridY;

        public DijkstrasAlgorithm(Node[,] grid, int gridX, int gridY, int total)
        {
            this.gridX = gridX;
            this.gridY = gridY;
            this.grid = grid;
            this.total = total;
        }

        public IEnumerable<Node> FindShortestPath(Node start, Node end)
        {
            start.Distance = 0;

            var priorityQueue = new PriorityQueue<Node, double>();
            var visited = new HashSet<Node>();

            foreach (var node in grid)
            {
                priorityQueue.Enqueue(node, node.Distance);
            }

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Dequeue();

                if (visited.Contains(current))
                {
                    continue;
                }

                visited.Add(current);

                if (visited.Count >= total && end.Previous != null) // Stopping condition: all nodes visited
                {
                    break;
                }

                foreach (var neighbor in GetForegroundNeighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        double newDist = current.Distance + 1;
                        if (newDist < neighbor.Distance)
                        {
                            neighbor.Distance = newDist;
                            neighbor.Previous = current;
                            priorityQueue.Enqueue(neighbor, neighbor.Distance);
                        }
                    }
                }

                foreach (var neighbor in GetForegroundDiagonalNeighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        double newDist = current.Distance + Math.Sqrt(2);
                        if (newDist < neighbor.Distance)
                        {
                            neighbor.Distance = newDist;
                            neighbor.Previous = current;
                            priorityQueue.Enqueue(neighbor, neighbor.Distance);
                        }
                    }
                }
            }

            return ReconstructPath(end);
        }

        private IEnumerable<Node> GetForegroundNeighbors(Node node)
        {
            var directions = new (int, int)[] { (0, 1), (1, 0), (0, -1), (-1, 0) };
            foreach (var (dx, dy) in directions)
            {
                int newX = node.X + dx, newY = node.Y + dy;
                if (newX >= 0 && newX < GridX && newY >= 0 && newY < GridY)
                {
                    var neighbor = grid[newY, newX];
                    if (neighbor.IsForeground)
                    {
                        yield return neighbor;
                    }
                }
            }
        }

        private IEnumerable<Node> GetForegroundDiagonalNeighbors(Node node)
        {
            var directions = new (int, int)[] { (-1, -1), (-1, 1), (1, -1), (1, 1) };
            foreach (var (dx, dy) in directions)
            {
                int newX = node.X + dx, newY = node.Y + dy;
                if (newX >= 0 && newX < GridX && newY >= 0 && newY < GridY)
                {
                    var neighbor = grid[newY, newX];
                    if (neighbor.IsForeground)
                    {
                        yield return neighbor;
                    }
                }
            }
        }

        private IEnumerable<Node> ReconstructPath(Node end)
        {
            var path = new Stack<Node>();
            var current = end;
            while (current != null)
            {
                path.Push(current);
                current = current.Previous;
            }
            return path;
        }
    }
}
