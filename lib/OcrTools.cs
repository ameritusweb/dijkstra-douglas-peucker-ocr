using Emgu.CV.Structure;
using Emgu.CV;
using ImageProcess.Model;
using System;
using System.Drawing;

namespace ImageProcess
{
    public class OcrTools
    {

        private Node[,] nodes;
        private int height;
        private int width;

        public OcrTools(Node[,] nodes)
        {
            this.nodes = nodes;
            this.height = nodes.GetLength(0);
            this.width = nodes.GetLength(1);
        }

        public OcrFeatures CalculateFeatures()
        {
            var boundingBox = CalculateBoundingBoxAndAspectRatio();
            var centerOfMassOffset = CalculateCenterOfMassOffset();
            IntersectionAnalyzer intersectionAnalyzer = new IntersectionAnalyzer();
            var (angles, percentages) = intersectionAnalyzer.Analyze(this.nodes, centerOfMassOffset.rawTotal);
            var negativeSpaceImage = FindNegativeSpace();
            ImageAnalyzer imageAnalyzer = new ImageAnalyzer(negativeSpaceImage);
            var metrics = imageAnalyzer.CalculateMetrics(150d);
            imageAnalyzer.Reset();
            var borderMetrics = imageAnalyzer.CalculateMetrics(220d);
            GridProcessor gridProcessor = new GridProcessor(negativeSpaceImage);
            var cornerPoints = gridProcessor.FindCornerPoints(); // topLeft, topRight, bottomLeft, bottomRight
            var paths = gridProcessor.ProcessGrid(cornerPoints);
            var longest = FindLongestLineSegmentSet(paths);
            DrawLineSegments(longest, negativeSpaceImage);
            LineSegmentProcessor lineSegmentProcessor = new LineSegmentProcessor();
            var totalLength = lineSegmentProcessor.CalculateTotalLength(longest);
            var segmentAngles = lineSegmentProcessor.CalculateAngles(longest);
            negativeSpaceImage.Save("E:\\images\\inputs\\ocr\\negativeSpaceImage.png");

            OcrFeatures features = new OcrFeatures();
            features.BoundingBox = new BoundingBox()
            {
                Width = boundingBox.maxX - boundingBox.minX,
                Height = boundingBox.maxY - boundingBox.minY
            };
            features.AspectRatio = boundingBox.aspectRatio;
            features.CentroidX = centerOfMassOffset.offsetX;
            features.CentroidY = centerOfMassOffset.offsetY;
            features.MassToTotalArea = centerOfMassOffset.massPercent;

            features.IntersectionArrays = new List<List<int>>();
            foreach (var angle in angles)
            {
                var i = angle.intersections;
                PadListToSize(i, 15, -1);
                features.IntersectionArrays.Add(i);
            }
            foreach (var percentage in percentages)
            {
                var p = percentage.Value.Select(x => (int)Math.Round(x)).ToList();
                PadListToSize(p, 15, -1);
                features.IntersectionArrays.Add(p);
            }
            PadListToSize(segmentAngles, 15, -1);
            features.LongestShortestPath = new ShortestPath()
            {
                 AngleChanges = segmentAngles.Take(15).ToList(),
                 TotalNumberOfLineSegments = longest.Count,
                 TotalLengthToDiagonalLengthRatio = totalLength / Math.Sqrt(Math.Pow(features.BoundingBox.Width, 2) + Math.Pow(features.BoundingBox.Height, 2)),
                 StartPosition = GetCornerPointPosition(cornerPoints, longest[0]),
                 EndPosition = GetCornerPointPosition(cornerPoints, longest[longest.Count - 1])
            };
            features.NumberOfNegativeSpaceBorders = borderMetrics.Count;
            features.NumberOfNegativeSpaces = metrics.Count;
            features.NegativeSpaces = metrics.ToList();
            features.NegativeSpaceBorders = borderMetrics.ToList();

            return features;
        }

        private string GetCornerPointPosition((Node topLeft, Node topRight, Node bottomLeft, Node bottomRight) cornerPoints, Node node)
        {
            if (node.Point.X == cornerPoints.topLeft.Point.X && node.Point.Y == cornerPoints.topLeft.Point.Y)
            {
                return "NW";
            } else if (node.Point.X == cornerPoints.topRight.Point.X && node.Point.Y == cornerPoints.topRight.Point.Y)
            {
                return "NE";
            } else if (node.Point.X == cornerPoints.bottomLeft.Point.X && node.Point.Y == cornerPoints.bottomLeft.Point.Y)
            {
                return "SW";
            } else if (node.Point.X == cornerPoints.bottomRight.Point.X && node.Point.Y == cornerPoints.bottomRight.Point.Y)
            {
                return "SE";
            } else
            {
                return "Unknown";
            }
        }

        public List<Node> FindLongestLineSegmentSet(IReadOnlyList<List<Node>> lineSegments)
        {
            double maxDistance = 0;
            List<Node> longestSegmentSet = null;

            foreach (var segmentSet in lineSegments)
            {
                double totalDistance = CalculateTotalDistance(segmentSet);
                if (totalDistance > maxDistance)
                {
                    maxDistance = totalDistance;
                    longestSegmentSet = segmentSet;
                }
            }

            return longestSegmentSet;
        }

        private double CalculateTotalDistance(List<Node> segmentSet)
        {
            double totalDistance = 0;
            for (int i = 0; i < segmentSet.Count - 1; i++)
            {
                totalDistance += EuclideanDistance(segmentSet[i], segmentSet[i + 1]);
            }
            return totalDistance;
        }

        private double EuclideanDistance(Node a, Node b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }

        public void DrawLineSegments(List<Node> nodes, Image<Gray, Byte> image)
        {
            // Check if there are enough nodes to form a line
            if (nodes.Count < 2)
            {
                Console.WriteLine("Insufficient nodes for line segments.");
                return;
            }

            // Loop through the nodes to draw line segments
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Point start = nodes[i].Point;
                Point end = nodes[i + 1].Point;

                // Draw the line segment on the image
                CvInvoke.Line(image, start, end, new Gray(100).MCvScalar);
            }
        }

        private Image<Gray, Byte> FindNegativeSpace()
        {
            var symbolPoints = this.nodes.GetForegroundPoints();
            List<Task> tasks = new List<Task>();

            Parallel.For(0, symbolPoints.Count, i =>
            {
                for (int j = i + 1; j < symbolPoints.Count; j++)
                {
                    var point1 = symbolPoints[i];
                    var point2 = symbolPoints[j];
                    ProcessPair(point1, point2);
                }
            });

            Image<Gray, Byte> finalImage = new Image<Gray, byte>(width, height);
            FinalizeImage(finalImage);

            return finalImage;
        }

        private void ProcessPair(Point point1, Point point2)
        {
            BounceLine(point1.X, point1.Y, point2.X, point2.Y);
        }

        private void BounceLine(int startX, int startY, int endX, int endY)
        {
            int dx = endX - startX;
            int dy = endY - startY;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));

            if (steps < 2)
            {
                return; // Ignore paths that are too short
            }

            float xIncrement = dx / (float)steps;
            float yIncrement = dy / (float)steps;

            float x = startX;
            float y = startY;
            bool hasBouncedOnce = false;

            for (int i = 0; i < steps * 2; i++)
            {
                int currentX = (int)Math.Round(x + xIncrement);
                int currentY = (int)Math.Round(y + yIncrement);

                if (currentX == startX && currentY == startY)
                {
                    break; // Stop if it has returned to the starting point
                }

                // Check for bounce conditions
                if (nodes[currentY, currentX].IsForeground)
                {
                    if (i == 0)
                    {
                        break;
                    }
                    if (!hasBouncedOnce)
                    {
                        hasBouncedOnce = true; // Mark that the line has bounced once
                                               // Reverse direction
                        x += xIncrement;
                        y += yIncrement;

                        xIncrement = -xIncrement;
                        yIncrement = -yIncrement;
                        continue;
                    }
                    else
                    {
                        break; // Stop if it has already bounced once
                    }
                }

                // Increment the NegativeSpaceValue only if it has bounced at least once
                if (hasBouncedOnce)
                {
                    InterlockedAdd(ref nodes[currentY, currentX].NegativeSpaceValue, 1);
                }

                x += xIncrement;
                y += yIncrement;
            }
        }

        private void InterlockedAdd(ref double location, double value)
        {
            double newCurrentValue;
            double currentValue = location;
            do
            {
                newCurrentValue = currentValue + value;
                currentValue = Interlocked.CompareExchange(ref location, newCurrentValue, currentValue);
            }
            while (currentValue != newCurrentValue);
        }

        private void FinalizeImage(Image<Gray, Byte> image)
        {
            // Iterate over the isProcessed array and update _image accordingly
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (nodes[y, x].NegativeSpaceValue > 0)
                    {
                        if (IsNextToBackgroundOrBorder(nodes, y, x))
                        {
                            image.Data[y, x, 0] = 220;
                        }
                        else
                        {
                            image.Data[y, x, 0] = 150;
                        }
                    }
                    else if (nodes[y, x].IsForeground)
                    {
                        image.Data[y, x, 0] = 50;
                    }
                    else
                    {
                        image.Data[y, x, 0] = 255;
                    }
                }
            }

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (image.Data[y, x, 0] == 0)
                    {
                        if ((y > 0 && image.Data[y - 1, x, 0] == 150)
                            ||
                            (x > 0 && image.Data[y, x - 1, 0] == 150)
                            ||
                            (x < nodes.GetLength(1) - 1 && image.Data[y, x + 1, 0] == 150)
                            ||
                            (y < nodes.GetLength(0) - 1 && image.Data[y + 1, x, 0] == 150))
                        {
                            image.Data[y, x, 0] = 0;
                        }
                        else
                        {
                            image.Data[y, x, 0] = 255;
                        }
                    }
                }
            }
        }

        private bool IsNextToBackgroundOrBorder(Node[,] nodes, int y, int x)
        {
            if (y == 0 || (!nodes[y - 1, x].IsForeground && !(nodes[y - 1, x].NegativeSpaceValue > 0)))
            {
                return true;
            }
            if (y == nodes.GetLength(0) - 1 || (!nodes[y + 1, x].IsForeground && !(nodes[y + 1, x].NegativeSpaceValue > 0)))
            {
                return true;
            }
            if (x == 0 || (!nodes[y, x - 1].IsForeground && !(nodes[y, x - 1].NegativeSpaceValue > 0)))
            {
                return true;
            }
            if (x == nodes.GetLength(1) - 1 || (!nodes[y, x + 1].IsForeground && !(nodes[y, x + 1].NegativeSpaceValue > 0)))
            {
                return true;
            }
            return false;
        }

        public (int minX, int minY, int maxX, int maxY, double aspectRatio) CalculateBoundingBoxAndAspectRatio()
        {
            int minX = width;
            int minY = height;
            int maxX = 0;
            int maxY = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (nodes[y, x].IsForeground)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (minX > maxX || minY > maxY)
            {
                // No `true` values found, return zero dimensions
                return (0, 0, 0, 0, 0.0);
            }

            // Calculate width and height of the bounding box
            int boxWidth = maxX - minX + 1;
            int boxHeight = maxY - minY + 1;

            // Calculate aspect ratio (width to height)
            double aspectRatio = (double)boxWidth / boxHeight;

            return (minX, minY, maxX, maxY, aspectRatio);
        }

        public (double offsetX, double offsetY, int rawTotal, double massPercent) CalculateCenterOfMassOffset()
        {
            int totalTrueCount = 0;
            double sumX = 0;
            double sumY = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (nodes[y, x].IsForeground)
                    {
                        sumX += x;
                        sumY += y;
                        totalTrueCount++;
                    }
                }
            }

            if (totalTrueCount == 0)
                return (0, 0, 0, 0); // Avoid division by zero if there are no `true` values

            double averageX = sumX / totalTrueCount;
            double averageY = sumY / totalTrueCount;

            // Calculate the center of the bitmap
            double centerX = width / 2.0;
            double centerY = height / 2.0;

            // Offset of the center of mass from the center of the bitmap
            double offsetX = averageX - centerX;
            double offsetY = averageY - centerY;

            return (offsetX, offsetY, totalTrueCount, totalTrueCount * 1d / (height * width) * 100d);
        }

        private void PadListToSize(List<int> list, int targetSize, int padValue)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            int paddingNeeded = targetSize - list.Count;
            for (int i = 0; i < paddingNeeded; i++)
            {
                list.Add(padValue);
            }
        }

        private void PadListToSize(List<double> list, int targetSize, int padValue)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            int paddingNeeded = targetSize - list.Count;
            for (int i = 0; i < paddingNeeded; i++)
            {
                list.Add(padValue);
            }
        }
    }
}
