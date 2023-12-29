namespace ImageProcess
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Threading.Tasks;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;

    public class NegativeSpaceFinderForOcr
    {
        private Node[,] _nodes;

        public void ProcessFile(string file, int index)
        {
            string outputDirectory = Path.Combine(Path.GetDirectoryName(file), "NewCharacters");
            if (index == 0)
            {
                DeleteFilesInDirectory(outputDirectory);
            }
            Image<Gray, Byte> img = ImageSerializer.DeserializeImage(file);
            Image<Gray, Byte> image = ResizeWithAntiAliasing(img.Mat).ToImage<Gray, Byte>();

            GridProcessor processor = new GridProcessor(image);
            var listOfLists = processor.ProcessGrid();

            var newImage = ProcessImage(image);

            var longest = FindLongestLineSegmentSet(listOfLists);
            DrawLineSegments(longest, newImage);

            string characterImagePath = Path.Combine(outputDirectory, $"new_character_{index}.png");
            newImage.Mat.Save(characterImagePath);

            //using VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hierarchy = new Mat();
            //CvInvoke.FindContours(newImage.Mat, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);

            //// Convert to color image for drawing in color
            //Mat colorImg = new Mat();
            //CvInvoke.CvtColor(newImage.Mat, colorImg, ColorConversion.Gray2Bgr);

            //// Draw all contours
            //CvInvoke.DrawContours(colorImg, contours, -1, new MCvScalar(0, 255, 0), 1);

            //string characterImagePath3 = Path.Combine(outputDirectory, $"bordered_{index}.png");
            //colorImg.Save(characterImagePath3);
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

        public Mat ResizeWithAntiAliasing(Mat inputImage)
        {
            // Apply a Gaussian blur to the image to reduce high-frequency noise (anti-aliasing)
            Mat blurredImage = new Mat();
            CvInvoke.GaussianBlur(inputImage, blurredImage, new Size(1, 1), 0);

            // Calculate the new size, which is double the original size
            Size newSize = new Size(inputImage.Cols * 2, inputImage.Rows * 2);

            // Resize the blurred image to the new size with cubic interpolation
            Mat resizedImage = new Mat();
            CvInvoke.Resize(blurredImage, resizedImage, newSize, interpolation: Inter.Cubic);

            return resizedImage;
        }

        public Image<Gray, Byte> ProcessImage(Image<Gray, Byte> image)
        {
            _nodes = DetectSymbolPoints(image);
            var symbolPoints = _nodes.GetForegroundPoints();
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

            Image<Gray, Byte> finalImage = image.Mat.ToImage<Gray, Byte>();
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
                if (_nodes[currentY, currentX].IsForeground)
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
                    InterlockedAdd(ref _nodes[currentY, currentX].NegativeSpaceValue, 1);
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
                    if (_nodes[y, x].NegativeSpaceValue > 0)
                    {
                        if (IsNextToBackgroundOrBorder(_nodes, y, x))
                        {
                            image.Data[y, x, 0] = 0;
                        } else
                        {
                            image.Data[y, x, 0] = 150;
                        }
                    } else if (_nodes[y, x].IsForeground)
                    {
                        image.Data[y, x, 0] = 220;
                    } else
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
                            (x < _nodes.GetLength(1) - 1 && image.Data[y, x + 1, 0] == 150)
                            ||
                            (y < _nodes.GetLength(0) - 1 && image.Data[y + 1, x, 0] == 150))
                        {
                            image.Data[y, x, 0] = 0;
                        } else
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

        private Node[,] DetectSymbolPoints(Image<Gray, Byte> image)
        {
            var nodes = new Node[image.Height, image.Width];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (image.Data[y, x, 0] <= 128)
                    {
                        nodes[y, x] = new Node() { IsForeground = true, Point = new Point() { X = x, Y = y } };
                    } else
                    {
                        nodes[y, x] = new Node();
                    }
                }
            }
            return nodes;
        }

        private void DeleteFilesInDirectory(string directoryPath)
        {
            try
            {
                // Get all file names in the directory
                string[] files = Directory.GetFiles(directoryPath);

                foreach (string file in files)
                {
                    // Delete each file
                    File.Delete(file);
                    Console.WriteLine($"Deleted file: {file}");
                }
            }
            catch (IOException ioExp)
            {
                Console.WriteLine($"An IO exception occurred: {ioExp.Message}");
            }
            catch (UnauthorizedAccessException unAuthExp)
            {
                Console.WriteLine($"UnauthorizedAccessException: {unAuthExp.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"An exception occurred: {e.Message}");
            }
        }
    }
}
