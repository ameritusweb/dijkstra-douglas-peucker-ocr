using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public class IntersectionAnalyzer
    {
        public IntersectionAnalyzer()
        {

        }

        public (List<Angle>, Dictionary<double, List<double>>) Analyze(Node[,] nodes, int totalCount)
        {
            int width = nodes.GetLength(1);
            int height = nodes.GetLength(0);
            var angleData = new Dictionary<double, List<int>>();
            List<Angle> angles = new List<Angle>();

            ConcurrentDictionary<(int Y, int X), List<(double A, int I)>> map = new ConcurrentDictionary<(int Y, int X), List<(double A, int I)>>();

            for (double angle = -90; angle < 270; angle += 3) // Adjust angle step as needed
            {
                var forwardSweep = CalculateSweep(nodes, width, height, angle, ref map);
                // var backwardSweep = CalculateSweep(angle, false);
                angleData[angle] = forwardSweep;
                if (angle % 3 == 0)
                {
                    angles.Add(new Angle((int)angle, forwardSweep));
                }
            }

            Dictionary<double, List<double>> percentageData = CalcPercentageDataFromMap(angleData, map, totalCount);
            return (angles, percentageData);
        }

        public Dictionary<double, List<double>> CalcPercentageDataFromMap(Dictionary<double, List<int>> angleData, ConcurrentDictionary<(int Y, int X), List<(double A, int I)>> map, int totalPixelCount)
        {
            var percentageData = new Dictionary<double, List<double>>();

            foreach (var angleEntry in angleData)
            {
                double angle = angleEntry.Key;
                var intersectionCounts = angleEntry.Value;
                var percentages = new List<double>(new double[intersectionCounts.Count]);

                for (int index = 0; index < intersectionCounts.Count; index++)
                {
                    int intersectionIndex = index;
                    var relevantPixels = map.Where(x => x.Value.Any(y => y.A == angle && y.I == intersectionIndex))
                                            .Select(x => x.Key)
                                            .Distinct()
                                            .Count();

                    double percentage = (double)relevantPixels / totalPixelCount * 100;
                    if (intersectionCounts[index] > 0)
                    {
                        percentages[index] = percentage;
                    }
                }

                percentageData[angle] = percentages;
            }

            return percentageData;
        }

        private List<int> CalculateSweep(Node[,] nodes, int width, int height, double angle, ref ConcurrentDictionary<(int Y, int X), List<(double A, int I)>> map)
        {
            double angleRad = angle * Math.PI / 180;
            int centerX = width / 2;
            int centerY = height / 2;
            int maxR = (int)Math.Sqrt(centerX * centerX + centerY * centerY);

            var rawIntersections = new int[maxR * 2];

            ConcurrentDictionary<int, List<(int Y, int X)>> set = new ConcurrentDictionary<int, List<(int Y, int X)>>();

            // Use Parallel.For for parallel processing
            Parallel.For(-maxR, maxR, (r) =>
            {
                int intersectionCount = 0;
                bool wasLastPixelWhite = true;
                int currentLength = 0;
                int localIndex = r + maxR;
                List<(int Y, int X)> points = new List<(int Y, int X)>();

                // Inner loop for sweeping the line across the bitmap
                for (int t = -maxR; t <= maxR; t++)
                {
                    // Calculate x, y using the line equation centered at the bitmap's center
                    int x = (int)Math.Round(centerX + (r * Math.Cos(angleRad) - t * Math.Sin(angleRad)));
                    int y = (int)Math.Round(centerY + (r * Math.Sin(angleRad) + t * Math.Cos(angleRad)));

                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        bool isPixelBlack = nodes[y, x].IsForeground;
                        if (isPixelBlack)
                        {
                            currentLength++;
                            points.Add((y, x));
                        }

                        if (wasLastPixelWhite && isPixelBlack)
                        {
                            wasLastPixelWhite = false;
                        }
                        else if (!wasLastPixelWhite && !isPixelBlack)
                        {
                            if (currentLength > 0)
                            {
                                intersectionCount++;
                                currentLength = 0;
                            }
                            wasLastPixelWhite = true;
                        }
                    }
                }

                set.AddOrUpdate(localIndex, points, (key, existingList) =>
                {
                    existingList.AddRange(points);
                    return existingList;
                });

                // Console.WriteLine($"angle: {angle}, r: {r}, count: {intersectionCount}");

                rawIntersections[localIndex] = intersectionCount;
            });

            return ConsolidateIntersections(rawIntersections, angle, ref set, ref map);
        }

        private List<int> ConsolidateIntersections(int[] rawIntersections, double angle, ref ConcurrentDictionary<int, List<(int Y, int X)>> set, ref ConcurrentDictionary<(int Y, int X), List<(double A, int I)>> map)
        {
            var consolidated = new List<int>();
            int? lastIntersectionCount = null;

            int lastIndex = -1;
            int i = 0;
            foreach (var count in rawIntersections.Take(rawIntersections.Length - 2))
            {
                if (lastIntersectionCount == null || lastIntersectionCount != count)
                {
                    if (count == rawIntersections[i + 1])
                    {
                        int cindex = consolidated.Count - 1;
                        consolidated.Add(count);
                        lastIntersectionCount = count;
                        List<(int Y, int X)> list = new List<(int Y, int X)>();
                        for (int j = lastIndex; j < i; j++)
                        {
                            if (j > -1)
                            {
                                list.AddRange(set[j]);
                            }
                        }
                        if (list.Any())
                        {
                            list = list.Distinct().ToList();
                            foreach (var val in list)
                            {
                                map.AddOrUpdate(val, new List<(double A, int I)> { (angle, cindex) }, (key, existingList) =>
                                {
                                    existingList.Add((angle, cindex));
                                    return existingList;
                                });
                            }
                        }
                        lastIndex = i;
                    }
                }
                i++;
            }
            return consolidated;
        }
    }
}
