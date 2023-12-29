namespace ImageProcess
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class CharacterAnalyzer
    {
        private bool[,] characterData;
        private int width;
        private int height;

        public CharacterAnalyzer(bool[,] data)
        {
            characterData = data;
            height = data.GetLength(0);
            width = data.GetLength(1);
        }

        public Dictionary<List<int>, decimal> AnalyzeCharacter(string fileName)
        {
            var angleData = new Dictionary<double, List<int>>();
            List<Angle> angles = new List<Angle>();
            var centerOffset = CalculateCenterOfMassOffset();
            var bounding = CalculateBoundingBoxAndAspectRatio();

            ConcurrentDictionary<(int Y, int X), List<(double A, int I)>> map = new ConcurrentDictionary<(int Y, int X), List<(double A, int I)>>();

            for (double angle = -90; angle < 270; angle += 3) // Adjust angle step as needed
            {
                var forwardSweep = CalculateSweep(angle, ref map);
                // var backwardSweep = CalculateSweep(angle, false);
                angleData[angle] = forwardSweep;
                if (angle % 3 == 0)
                {
                    angles.Add(new Angle((int)angle, forwardSweep));
                }
            }

            Dictionary<double, List<double>> percentageData = CalcPercentageDataFromMap(angleData, map, centerOffset.totalCount);
            var ii = CountIntersectionArrays(angleData);
            var percentages = CalculatePercentages(ii);
            var charData = new CharacterData()
            {
                AngleList = angles,
                CenterOfMassOffset = centerOffset,
                Percentages = percentages,
                AspectRatio = bounding.aspectRatio
            };
             var normalized = charData.Normalize();
            var json = JsonConvert.SerializeObject(percentages, new PercentageDictionaryConverter());
            File.WriteAllText($"E:\\images\\saved_images_chars\\{fileName}.json", json);
            File.WriteAllText($"E:\\images\\saved_images_chars\\{fileName}_2.json", 
                JsonConvert.SerializeObject(charData));
            //File.WriteAllText($"E:\\images\\saved_images_chars\\norm_{fileName}.json",
            //    JsonConvert.SerializeObject(normalized));
            return percentages;
        }

        public static double CalculateSequenceAlignmentKernel(int[] sequence1, int[] sequence2, int mismatchPenalty, int gapPenalty)
        {
            int length1 = sequence1.Length;
            int length2 = sequence2.Length;
            int[,] alignmentMatrix = new int[length1 + 1, length2 + 1];

            // Initialize the first row and column for gap penalties
            for (int i = 0; i <= length1; i++)
                alignmentMatrix[i, 0] = -i * gapPenalty;

            for (int j = 0; j <= length2; j++)
                alignmentMatrix[0, j] = -j * gapPenalty;

            // Fill the alignment matrix

            for (int i = 1; i <= length1; i++)
            {
                for (int j = 1; j <= length2; j++)
                {
                    int matchScore = (sequence1[i - 1] == sequence2[j - 1]) ? 1 : -mismatchPenalty;
                    int match = alignmentMatrix[i - 1, j - 1] + matchScore;
                    int delete = alignmentMatrix[i - 1, j] - gapPenalty;
                    int insert = alignmentMatrix[i, j - 1] - gapPenalty;
                    alignmentMatrix[i, j] = Math.Max(Math.Max(match, delete), insert);
                }
            }

            // Summation of all positive values in the matrix for local alignments
            double sum = 0;
            for (int i = 1; i <= length1; i++)
                for (int j = 1; j <= length2; j++)
                    if (alignmentMatrix[i, j] > 0)
                        sum += alignmentMatrix[i, j];

            // Normalization (optional)
            sum /= (length1 + length2);

            return sum;
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


        public (double offsetX, double offsetY, int totalCount) CalculateCenterOfMassOffset()
        {
            int totalTrueCount = 0;
            double sumX = 0;
            double sumY = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (characterData[y, x])
                    {
                        sumX += x;
                        sumY += y;
                        totalTrueCount++;
                    }
                }
            }

            if (totalTrueCount == 0)
                return (0, 0, 0); // Avoid division by zero if there are no `true` values

            double averageX = sumX / totalTrueCount;
            double averageY = sumY / totalTrueCount;

            // Calculate the center of the bitmap
            double centerX = width / 2.0;
            double centerY = height / 2.0;

            // Offset of the center of mass from the center of the bitmap
            double offsetX = averageX - centerX;
            double offsetY = averageY - centerY;

            return (offsetX, offsetY, totalTrueCount);
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
                    if (characterData[y, x])
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


        private Dictionary<string, int> CountIntersectionArrays(Dictionary<double, List<int>> angleData)
        {
            var countDict = new Dictionary<string, int>();

            foreach (var entry in angleData)
            {
                string key = string.Join(",", entry.Value);
                if (countDict.ContainsKey(key))
                {
                    countDict[key]++;
                }
                else
                {
                    countDict[key] = 1;
                }
            }

            return countDict;
        }

        private Dictionary<List<int>, decimal> CalculatePercentages(Dictionary<string, int> countDict)
        {
            int totalCount = countDict.Values.Sum();
            var percentages = new Dictionary<List<int>, decimal>();

            foreach (var pair in countDict)
            {
                decimal percentage = (decimal)pair.Value / totalCount * 100;
                if (percentage < 5m)
                {
                    totalCount -= pair.Value;
                }
            }

            foreach (var pair in countDict)
            {
                List<int> array = pair.Key.Split(',').Select(int.Parse).ToList();
                decimal percentage = (decimal)pair.Value / totalCount * 100;
                if (percentage >= 7.5m)
                {
                    percentages[array] = percentage;
                }
            }

            return percentages;
        }


        private List<int> CalculateSweep(double angle, ref ConcurrentDictionary<(int Y, int X), List<(double A, int I)>> map)
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
                        bool isPixelBlack = characterData[y, x];
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
