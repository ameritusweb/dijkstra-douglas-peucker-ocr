using ImageProcess.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return features;
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
    }
}
