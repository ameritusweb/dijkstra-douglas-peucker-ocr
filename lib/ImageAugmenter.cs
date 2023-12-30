namespace ImageProcess
{
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using System;
    using System.Drawing;

    public class ImageAugmenter
    {
        private Random _random = new Random(Guid.NewGuid().GetHashCode());

        public void ApplyRandomRotationWithSelectiveReversal(string inputFilePath, string outputFilePath)
        {
            Mat imgColor = CvInvoke.Imread(inputFilePath, Emgu.CV.CvEnum.ImreadModes.Unchanged);
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

            Image<Gray, Byte> originalImage = ResizeWithAntiAliasing(newImage.Mat).ToImage<Gray, Byte>();

            // Clone the original image for rotation
            var rotatedImage = originalImage.Clone();

            // Apply random rotation
            double angle = _random.Next(-45, 45); // Random angle between -45 and 45 degrees
            var rotationMatrix = new RotationMatrix2D(new PointF(rotatedImage.Width / 2f, rotatedImage.Height / 2f), angle, 1.0);
            CvInvoke.WarpAffine(rotatedImage, rotatedImage, rotationMatrix, rotatedImage.Size, Inter.Linear, Warp.Default, BorderType.Replicate);

            // Calculate the difference
            var differenceImage = new Image<Gray, byte>(rotatedImage.Size);
            CvInvoke.AbsDiff(originalImage.Convert<Gray, byte>(), rotatedImage.Convert<Gray, byte>(), differenceImage);

            // Randomly determine the percentage of changes to reverse
            double removalPercent = _random.NextDouble(); // Random percentage between 0 and 1

            // Threshold for selective reversal
            double threshold = removalPercent * 255; // Assuming pixel values range from 0 to 255

            // Iterate through the difference image
            for (int y = 0; y < differenceImage.Height; y++)
            {
                for (int x = 0; x < differenceImage.Width; x++)
                {
                    // If the change is below the threshold, revert the change
                    if (differenceImage.Data[y, x, 0] < threshold)
                    {
                        rotatedImage.Data[y, x, 0] = originalImage.Data[y, x, 0];
                    }
                }
            }

            // Save the modified image
            rotatedImage.Save(outputFilePath);
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
