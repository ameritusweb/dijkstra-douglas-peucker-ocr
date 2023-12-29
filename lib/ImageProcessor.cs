using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Collections.Concurrent;
using System.Drawing;

namespace ImageProcess
{
    public class ImageProcessor
    {

        private int charIndex = 0;

        public void ProcessImageCV(string imagePath)
        {
            string outputDirectory = Path.Combine(Path.GetDirectoryName(imagePath), "Characters");
            DeleteFilesInDirectory(outputDirectory);

            Mat imgColor = CvInvoke.Imread(imagePath, Emgu.CV.CvEnum.ImreadModes.Color);
            Image<Bgr, Byte> imgColor2 = imgColor.ToImage<Bgr, Byte>();

            Image<Gray, Byte> newImageColor = new Image<Gray, Byte>(imgColor.Width, imgColor.Height, new Gray(255));

            Parallel.For(0, imgColor.Height, y =>
            {
                for (int x = 0; x < imgColor.Width; x++)
                {
                    if (imgColor2[y, x].Green != imgColor2[y, x].Blue || imgColor2[y, x].Blue != imgColor2[y, x].Red || imgColor2[y, x].Green != imgColor2[y, x].Red)
                    {
                        newImageColor[y, x] = new Gray(Math.Min(Math.Min(imgColor2[y, x].Green, imgColor2[y, x].Blue), imgColor2[y, x].Red));
                    }
                    else
                    {
                        newImageColor[y, x] = new Gray(imgColor2[y, x].Green);
                    }
                }
            });

            Mat img = new Mat();
            Mat img2 = newImageColor.Mat; //CvInvoke.Imread(imagePath, Emgu.CV.CvEnum.ImreadModes.Grayscale);
            CvInvoke.Threshold(img2, img, 230, 255, ThresholdType.Binary);

            Mat img3 = new Mat();
            CvInvoke.Threshold(img2, img3, 155, 255, ThresholdType.ToZero);

            string characterImagePath4 = Path.Combine(outputDirectory, $"bordered155.png");
            img3.Save(characterImagePath4);

            using VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hierarchy = new Mat();
            CvInvoke.FindContours(img, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);

            // Convert to color image for drawing in color
            Mat colorImg = new Mat();
            CvInvoke.CvtColor(img, colorImg, ColorConversion.Gray2Bgr);

            // Draw all contours
            CvInvoke.DrawContours(colorImg, contours, -1, new MCvScalar(0, 255, 0), 1);

            string characterImagePath3 = Path.Combine(outputDirectory, $"bordered.png");
            colorImg.Save(characterImagePath3);

            var ImageData = colorImg.ToImage<Bgr, Byte>(true);

            // Create an empty image with the same size as the original
            Image<Bgr, Byte> newImage = new Image<Bgr, Byte>(ImageData.Width, ImageData.Height, new Bgr(255, 255, 255));

            for (int y = 0; y < ImageData.Height; y++)
            {
                for (int x = 0; x < ImageData.Width; x++)
                {
                    if (IsBlackAdjacentToGreen(ImageData, x, y))
                    {
                        newImage[y, x] = new Bgr(0, 0, 0); // Set to black
                    }
                }
            }

            string characterImagePath2 = Path.Combine(outputDirectory, $"bordered_next.png");
            newImage.Save(characterImagePath2);

            var groups = ProcessGroups(newImage);

            // Create an empty image with the same size as the original
            var ImageData2 = img.ToImage<Bgr, Byte>(true);
            Image<Bgr, Byte> newImage2 = new Image<Bgr, Byte>(ImageData2.Width, ImageData2.Height, new Bgr(255, 255, 255));

            charIndex = 0;
            foreach (var gr in groups.Values)
            {
                // Extract character image
                Mat characterMat = new Mat(img3, gr.BoundingBox);
                bool res = SplitMat(characterMat, img3, gr.BoundingBox, outputDirectory);
                if (!res)
                {
                    string characterImagePath = Path.Combine(outputDirectory, $"character1_{charIndex++}.png");
                    characterMat.Save(characterImagePath);
                }   
            }
        }

        public bool SplitMat(Mat characterMat, Mat img3, Rectangle boundingBox, string outputDirectory)
        {
            Image<Bgr, Byte> charImage = characterMat.ToImage<Bgr, Byte>(true);
            double[] columnWhitespacePercentages = CalculateColumnWhitespacePercentages(charImage);
            if (columnWhitespacePercentages.Any(x => x > 0))
            {

                List<int> splitIndices = GetSplitIndices(columnWhitespacePercentages, charImage.Width < charImage.Height ? 0.98 : 0.95);

                Rectangle[] splitBoxes = SplitBoundingBox(boundingBox, splitIndices);

                // Save the character images for each split box
                foreach (var box in splitBoxes)
                {
                    if (box.Width > 1)
                    {
                        Mat splitMat = new Mat(img3, box);
                        bool res = box.Width == boundingBox.Width && box.Height == boundingBox.Height ? false : SplitMat(splitMat, img3, box, outputDirectory);
                        if (!res)
                        {
                            string splitCharacterImagePath = Path.Combine(outputDirectory, $"character_{charIndex++}.png");
                            splitMat.Save(splitCharacterImagePath);
                        }
                    }
                }
                return true;
            }

            return false;
        }

        public double[] CalculateColumnWhitespacePercentages(Image<Bgr, Byte> image)
        {
            double[] whitespacePercentages = new double[image.Width];
            if (image.Width >= 6)
            {
                for (int x = (int)(image.Width / 4d); x < image.Width - ((int)(image.Width / 4d)); x++)
                {
                    double whiteCount = 0;
                    bool hasZero = false;
                    double max = 0;
                    for (int y = 0; y < image.Height; y++)
                    {
                        double min = Math.Min(Math.Min(image[y, x].Blue, image[y, x].Green), image[y, x].Red);
                        if (min > max)
                        {
                            max = min;
                        }
                    }
                    for (int y = 0; y < image.Height; y++)
                    {
                        double min = Math.Min(Math.Min(image[y, x].Blue, image[y, x].Green), image[y, x].Red);
                        if (image[y, x].Blue == 0 && image[y, x].Green == 0 && image[y, x].Red == 0)
                            hasZero = true;
                        var count = min / max;
                        whiteCount += count;
                    }
                    if (!hasZero)
                        whitespacePercentages[x] = whiteCount / image.Height;
                }
            }
            return whitespacePercentages;
        }

        public List<int> GetSplitIndices(double[] whitespacePercentages, double threshold)
        {
            List<int> indices = new List<int>();
            for (int i = 1; i < whitespacePercentages.Length - 1; i++)
            {
                if (whitespacePercentages[i] > threshold && ((whitespacePercentages[i-1] == 0 || whitespacePercentages[i - 1] > threshold) || (whitespacePercentages[i + 1] == 0 || whitespacePercentages[i + 1] > threshold)))
                {
                    indices.Add(i);
                    if (indices.Count == 2) break; // Limit to two splits
                }
            }
            if (!indices.Any())
            {
                for (int i = 1; i < whitespacePercentages.Length - 1; i++)
                {
                    if (whitespacePercentages[i] > threshold && ((whitespacePercentages[i - 1] == 0 || whitespacePercentages[i - 1] > threshold) || (whitespacePercentages[i + 1] == 0 || whitespacePercentages[i + 1] > threshold)))
                    {
                        indices.Add(i);
                        if (indices.Count == 2) break; // Limit to two splits
                    }
                }
            }
            if (!indices.Any() && whitespacePercentages.Any(x => x > 0))
            {

            }
            return indices;
        }

        public Rectangle[] SplitBoundingBox(Rectangle boundingBox, List<int> splitIndices)
        {
            if (splitIndices.Count == 0)
            {
                return new Rectangle[] { boundingBox };
            }
            else if (splitIndices.Count == 1)
            {
                return new Rectangle[]
                {
            new Rectangle(boundingBox.X, boundingBox.Y, splitIndices[0], boundingBox.Height),
            new Rectangle(boundingBox.X + splitIndices[0], boundingBox.Y, boundingBox.Width - splitIndices[0], boundingBox.Height)
                };
            }
            else // Two split points
            {
                return new Rectangle[]
                {
            new Rectangle(boundingBox.X, boundingBox.Y, splitIndices[0], boundingBox.Height),
            new Rectangle(boundingBox.X + splitIndices[0], boundingBox.Y, splitIndices[1] - splitIndices[0], boundingBox.Height),
            new Rectangle(boundingBox.X + splitIndices[1], boundingBox.Y, boundingBox.Width - splitIndices[1], boundingBox.Height)
                };
            }
        }

        private ConcurrentDictionary<int, Group> ProcessGroups(Image<Bgr, Byte> image)
        {
            // Initialize UnionFind and labels array
            UnionFind uf = new UnionFind(image.Width * image.Height);
            int[,] labels = new int[image.Height, image.Width];
            ConcurrentDictionary<int, Group> groups = new ConcurrentDictionary<int, Group>();

            // First pass: Assign initial labels and merge with neighbors
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (image[y, x].Equals(new Bgr(255, 255, 255))) // Skip white pixels
                        continue;

                    int currentIndex = GetIndex(x, y, image.Width);
                    labels[y, x] = currentIndex; // Assign initial label

                    // Merge with neighboring pixels if necessary
                    MergeWithNeighbors(uf, labels, x, y, currentIndex);
                }
            }

            // Second pass: Update final labels and bounding boxes
            Parallel.For(0, image.Height, y =>
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (labels[y, x] != 0)
                    {
                        int finalLabel = uf.Find(GetIndex(x, y, image.Width));
                        labels[y, x] = finalLabel; // Update to final label

                        // Update bounding boxes
                        UpdateBoundingBox(groups, finalLabel, x, y);
                    }
                }
            });

            RemoveNestedBoundingBoxes(groups);

            return groups;
        }

        private void RemoveNestedBoundingBoxes(ConcurrentDictionary<int, Group> groups)
        {
            var groupKeysToRemove = new List<int>();

            foreach (var group1 in groups)
            {
                foreach (var group2 in groups)
                {
                    if (group1.Key != group2.Key && IsBoundingBoxWithin(group1.Value.BoundingBox, group2.Value.BoundingBox))
                    {
                        groupKeysToRemove.Add(group1.Key);
                        break;
                    }
                }
            }

            foreach (var key in groupKeysToRemove.Distinct())
            {
                groups.TryRemove(key, out _);
            }
        }

        private bool IsBoundingBoxWithin(Rectangle inner, Rectangle outer)
        {
            return outer.Contains(inner);
        }

        private void MergeWithNeighbors(UnionFind uf, int[,] labels, int x, int y, int currentLabel)
        {
            int width = labels.GetLength(1);
            int height = labels.GetLength(0);

            // Calculate the index of the current pixel
            int currentIndex = y * width + x;

            // Check left neighbor
            if (x > 0)
            {
                int leftLabel = labels[y, x - 1];
                if (leftLabel != 0) // Ensure it's part of a group
                {
                    int leftIndex = y * width + (x - 1);
                    uf.Union(currentIndex, leftIndex);
                }
            }

            // Check top neighbor
            if (y > 0)
            {
                int topLabel = labels[y - 1, x];
                if (topLabel != 0) // Ensure it's part of a group
                {
                    int topIndex = (y - 1) * width + x;
                    uf.Union(currentIndex, topIndex);
                }
            }
        }

        private void UpdateBoundingBox(ConcurrentDictionary<int, Group> groups, int label, int x, int y)
        {
            groups.AddOrUpdate(label,
                // Add logic
                (lbl) => new Group { BoundingBox = new Rectangle(x, y, 1, 1) },
                // Update logic
                (lbl, group) => {
                    var bbox = new Rectangle(
                        Math.Min(group.BoundingBox.X, x),
                        Math.Min(group.BoundingBox.Y, y),
                        Math.Max(group.BoundingBox.Right, x + 1) - Math.Min(group.BoundingBox.X, x),
                        Math.Max(group.BoundingBox.Bottom, y + 1) - Math.Min(group.BoundingBox.Y, y));
                    return new Group { BoundingBox = bbox };
                });
        }

        public int GetIndex(int x, int y, int imageWidth)
        {
            return y * imageWidth + x;
        }


        private bool IsBlackAdjacentToGreen(Image<Bgr, Byte> img, int x, int y)
        {
            Bgr black = new Bgr(0, 0, 0);
            Bgr green = new Bgr(0, 255, 0); // Note: OpenCV uses BGR format

            if (!img[y, x].Equals(black))
            {
                if (img[y, x].Equals(green) && y > 0 && x > 0 && y < img.Height - 1 && x < img.Width - 1)
                {
                    if (img[y + 1, x].Equals(black) && img[y - 1, x].Equals(black) && (img[y, x + 1].Equals(black) || img[y, x - 1].Equals(black)))
                    {
                        return true;
                    }
                    else if (img[y, x + 1].Equals(black) && img[y, x - 1].Equals(black) && (img[y + 1, x].Equals(black) || img[y - 1, x].Equals(black)))
                    {
                        return true;
                    }
                }
                return false;
            }

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip the current pixel

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < img.Width && ny >= 0 && ny < img.Height)
                    {
                        Bgr neighborColor = img[ny, nx];
                        if (neighborColor.Equals(green))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void DeleteFilesInDirectory(string directoryPath)
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

        public bool[,] ConvertImageToBoolArray(string imagePath)
        {
            using (Bitmap image = new Bitmap(imagePath))
            {
                bool[,] boolArray = new bool[image.Height, image.Width];

                for (int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        Color pixelColor = image.GetPixel(x, y);
                        boolArray[y, x] = !IsWhite(pixelColor);
                    }
                }

                return boolArray;
            }
        }

        private bool IsWhite(Color color)
        {
            // Assuming white is defined as RGB(255, 255, 255)
            return color.A == 0 ||
            (color.R == 255 && color.G == 255 && color.B == 255);
        }
    }
}
