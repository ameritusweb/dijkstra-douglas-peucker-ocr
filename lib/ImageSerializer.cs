namespace ImageProcess
{
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using System.Drawing;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public class ImageSerializer
    {
        public static void SerializeImage(Image<Gray, Byte> image, string filePath)
        {
            byte[] imageData = image.Mat.ToImage<Gray, Byte>().ToJpegData(); // Convert to JPEG byte array
            File.WriteAllBytes(filePath, imageData);
        }

        public static Image<Gray, Byte> ConvertToImage(Node[,] grid)
        {
            int height = grid.GetLength(0);
            int width = grid.GetLength(1);

            Image<Gray, Byte> img = new Image<Gray, Byte>(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Node node = grid[y, x];
                    img.Data[y, x, 0] = (byte)node.Intensity;
                }
            }

            return img;
        }


        public static Image<Gray, Byte> DeserializeImage(string filePath)
        {
            Image<Gray, Byte> image = new Image<Gray, Byte>(filePath);
            return image;
        }

        public static Node[,] DeserializeImageWithAntiAlias(string filePath)
        {
            Mat imgColor = CvInvoke.Imread(filePath, Emgu.CV.CvEnum.ImreadModes.Unchanged);
            Image<Bgra, Byte> imgColor2 = imgColor.ToImage<Bgra, Byte>();

            Image<Gray, Byte> newImage = new Image<Gray, Byte>(imgColor.Width, imgColor.Height, new Gray(255));

            Parallel.For(0, imgColor.Height, y =>
            {
                for (int x = 0; x < imgColor.Width; x++)
                {
                    if (imgColor2[y, x].Alpha == 0)
                    {
                        newImage[y, x] = new Gray(255);
                        continue;
                    }
                    if (imgColor2[y, x].Green != imgColor2[y, x].Blue || imgColor2[y, x].Blue != imgColor2[y, x].Red || imgColor2[y, x].Green != imgColor2[y, x].Red)
                    {
                        newImage[y, x] = new Gray(Math.Min(Math.Min(imgColor2[y, x].Green, imgColor2[y, x].Blue), imgColor2[y, x].Red));
                    }
                    else
                    {
                        newImage[y, x] = new Gray(imgColor2[y, x].Green);
                    }
                }
            });

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            Image<Gray, Byte> img = ResizeWithAntiAliasing(newImage.Mat).ToImage<Gray, Byte>();
            var grid = new Node[img.Height, img.Width];
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    bool isForeground = img.Data[y, x, 0] <= 128;
                    grid[y, x] = new Node(y, x, isForeground);
                    if (isForeground)
                    {
                        grid[y, x].Intensity = img.Data[y, x, 0];
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    } else
                    {
                        grid[y, x].Intensity = 255;
                    }
                }
            }

            // Adjust for 1-pixel border
            minX = Math.Max(0, minX - 1);
            minY = Math.Max(0, minY - 1);
            maxX = Math.Min(img.Width - 1, maxX + 1);
            maxY = Math.Min(img.Height - 1, maxY + 1);

            int newWidth = maxX - minX + 1;
            int newHeight = maxY - minY + 1;

            var postGrid = new Node[newHeight, newWidth];

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    // Adjusting the coordinates for the new grid
                    int newY = y - minY;
                    int newX = x - minX;
                    postGrid[newY, newX] = new Node(newY, newX, grid[y, x].IsForeground);
                }
            }

            return postGrid;
        }

        public static Mat ResizeWithAntiAliasing(Mat inputImage)
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
    }

}
