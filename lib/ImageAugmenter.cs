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

        public void ApplyRandomRotationWithSelectiveReversal(string inputFilePath, double rotationAngle)
        {
            Mat imgColor = CvInvoke.Imread(inputFilePath, Emgu.CV.CvEnum.ImreadModes.Unchanged);
            Image<Bgra, Byte> originalImage = imgColor.ToImage<Bgra, Byte>();

            // Clone the original image for rotation
            var rotatedImage = originalImage.Clone();

            // Apply random rotation
            double angle = rotationAngle;
            var rotationMatrix = new RotationMatrix2D(new PointF(rotatedImage.Width / 2f, rotatedImage.Height / 2f), angle, 1.0);
            CvInvoke.WarpAffine(rotatedImage, rotatedImage, rotationMatrix, rotatedImage.Size, Inter.Linear, Warp.Default, BorderType.Replicate);

            // Save the modified image
            if (angle > 0)
            {
                rotatedImage.Save(inputFilePath.Replace(".png", "_pos" + Math.Abs(angle) + ".png"));
            }
            else
            {
                rotatedImage.Save(inputFilePath.Replace(".png", "_neg" + Math.Abs(angle) + ".png"));
            }
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
