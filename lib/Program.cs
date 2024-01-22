using Emgu.CV.Structure;
using Emgu.CV;
using Newtonsoft.Json;
using System.Drawing;
using System.Text.Json.Serialization;
using Emgu.CV.CvEnum;
using ImageProcess.Model;

namespace ImageProcess
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var afiles = Directory.GetFiles("E:\\images\\saved_images_D", "*.png");
            //foreach (var afile in afiles)
            //{
            //    ImageAugmenter imageAugmenter = new ImageAugmenter();
            //    for (int i = -10; i <= 10; i++)
            //    {
            //        if (i != 0)
            //            imageAugmenter.ApplyRandomRotationWithSelectiveReversal(afile, i);
            //    }
            //}

            var cFiles = Directory.GetFiles("E:\\images\\inputs\\Ocr", "char*.png");
            FeatureScaler scaler = new FeatureScaler();
            foreach (var cFile in cFiles)
            {
                Node[,] nodes = ImageSerializer.DeserializeImageWithAntiAlias(cFile);
                Node[,] interpolated = ImageSerializer.BilinearInterpolation(nodes, 50, 50);
                var img = ImageSerializer.ConvertToImageGrayscale(nodes);
                ImageSerializer.SerializeImage(img, cFile.Replace(".png", "_interpolated.png"));
                OcrTools tools = new OcrTools(nodes);
                OcrFeatures features = tools.CalculateFeatures();
                var vector = scaler.ScaleFeatures(features);
                List<List<double>> list = new List<List<double>>();
                for (int i = 0; i < 17; ++i)
                {
                    var subvect = vector.Skip(i * 223).Take(223).ToList();
                    list.Add(subvect);
                }
                //var json = JsonConvert.SerializeObject(list, Formatting.Indented);
                //File.WriteAllText(cFile.Replace(".png", ".json"), json);
                //scaler.UpdateMinMax(features);
            }
            //scaler.ExportMinMaxValues("E:\\images\\inputs\\Ocr\\MinMaxValues.json");

            var charFiles = Directory.GetFiles("E:\\images\\inputs\\Characters", "char*.png");

            //var sFiles = Directory.GetFiles("E:\\images\\saved_images", "*.png");
            //ConcatenateImagesHorizontally(sFiles.ToList(), 100, 100).Mat.Save("E:\\images\\saved_images\\concat.png");

            var index = 0;
            foreach (var charFile in charFiles)
            {
                NegativeSpaceFinderForOcr enclosedAndNegativeSpaceFinderForOcr = new NegativeSpaceFinderForOcr();
                enclosedAndNegativeSpaceFinderForOcr.ProcessFile(charFile, index++);
            }

            var inputs = Directory.GetFiles("E:\\images\\inputs", "o_*.png");

            var files = Directory.GetFiles("E:\\images\\saved_images", "*.png");

            ImageProcessor imageProcessor = new ImageProcessor();

            foreach (var input in inputs)
            {
                imageProcessor.ProcessImageCV(input);
            }

            CharacterData.MaxValue = decimal.MinValue;
            CharacterData.MinValue = decimal.MaxValue;
            foreach (var file in files)
            {
                //FileInfo fileInfo = new FileInfo(file);
                //var boolArray = imageProcessor.ConvertImageToBoolArray(file);
                //var reform = imageProcessor.ReformulateArrayWithPadding(boolArray);

                //CharacterAnalyzer characterAnalyzer = new CharacterAnalyzer(reform);
                //var map = characterAnalyzer.AnalyzeCharacter(fileInfo.Name);

                 //var bmp = imageProcessor.CreateBitmapFromBoolArray(reform);
                 //bmp.Save($"E:\\images\\saved_images_reform\\{fileInfo.Name}");
                // var json = JsonConvert.SerializeObject(reform);
                // File.WriteAllText($"E:\\images\\saved_images_json\\{fileInfo.Name}.json", json);
            }
        }

        public static Image<Bgr, byte> ConcatenateImagesHorizontally(List<string> imagePaths, int width, int height)
        {
            int totalWidth = 0;

            // Load all images and check they're the same height
            List<Image<Bgr, byte>> images = new List<Image<Bgr, byte>>();
            foreach (var path in imagePaths)
            {
                Image<Bgr, byte> img = new Image<Bgr, byte>(width, height);
                using (Bitmap image = new Bitmap(path))
                {

                    for (int x = 0; x < image.Width; x++)
                    {
                        for (int y = 0; y < image.Height; y++)
                        {
                            Color pixelColor = image.GetPixel(x, y);
                            if (pixelColor.A == 0)
                            {
                                pixelColor = Color.White;
                            }
                            img.Data[y, x, 0] = pixelColor.B;
                            img.Data[y, x, 1] = pixelColor.G;
                            img.Data[y, x, 2] = pixelColor.R;
                        }
                    }
                }

                images.Add(img);
                totalWidth += img.Width;
            }

            var startImage = images[0];
            foreach (var img in images.Skip(1))
            {
                startImage = startImage.ConcateHorizontal(img);
            }

            return startImage;
        }
    }
}