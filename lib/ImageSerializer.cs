namespace ImageProcess
{
    using Emgu.CV;
    using Emgu.CV.Structure;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public class ImageSerializer
    {
        public static void SerializeImage(Image<Gray, Byte> image, string filePath)
        {
            byte[] imageData = image.Mat.ToImage<Gray, Byte>().ToJpegData(); // Convert to JPEG byte array
            File.WriteAllBytes(filePath, imageData);
        }

        public static Image<Gray, Byte> DeserializeImage(string filePath)
        {
            Image<Gray, Byte> image = new Image<Gray, Byte>(filePath);
            return image;
        }
    }

}
