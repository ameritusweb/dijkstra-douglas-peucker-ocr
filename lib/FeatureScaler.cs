using ImageProcess.Model;
using Newtonsoft.Json;

namespace ImageProcess
{
    public class FeatureScaler
    {
        private Dictionary<string, double> minValues;
        private Dictionary<string, double> maxValues;

        // Define mappings for StartPosition and EndPosition strings to scaled values
        private Dictionary<string, int> positionMapping = new Dictionary<string, int>
        {
            { "NW", 1 },
            { "SW", 2 },
            { "NE", 3 },
            { "SE", 4 }
        };

        public FeatureScaler()
        {
            var data = new
            {
                MinValues = new
                {
                    AspectRatio = 0.07407407407407407,
                    CentroidX = -8.944444444444445,
                    CentroidY = -7.5588235294117645,
                    MassToTotalArea = 19.00826446280992,
                    IntersectionArrays = -1.0,
                    NegativeSpacesCount = 0.0,
                    NegativeSpaceBordersCount = 0.0,
                    TotalLengthToDiagonalLengthRatio = 0.13266999603886284,
                    TotalNumberOfLineSegments = 2.0,
                    AngleChanges = -1.0,
                    StartPosition = 1.0,
                    EndPosition = 2.0,
                    Circularity = 0.10315757770275126,
                    CentroidPositionX = -9.642857142857142,
                    CentroidPositionY = -9.583333333333334,
                    RelativeMass = 0.5016722408026756,
                    RelativeMax = 0.0,
                    MajorAxisLength = 0.0,
                    MinorAxisLength = 0.0,
                    AxisAngle = -1.5707963267948966
                },
                MaxValues = new
                {
                    AspectRatio = 9.0,
                    CentroidX = 5.009803921568629,
                    CentroidY = 5.833333333333332,
                    MassToTotalArea = 80.0,
                    IntersectionArrays = 100.0,
                    NegativeSpacesCount = 9.0,
                    NegativeSpaceBordersCount = 8.0,
                    TotalLengthToDiagonalLengthRatio = 1.807238806363898,
                    TotalNumberOfLineSegments = 15.0,
                    AngleChanges = 75.32360686255,
                    StartPosition = 3.0,
                    EndPosition = 4.0,
                    Circularity = 12.566370614359172,
                    CentroidPositionX = 9.285714285714286,
                    CentroidPositionY = 9.166666666666666,
                    RelativeMass = 48.538961038961034,
                    RelativeMax = 44.648225634257656,
                    MajorAxisLength = 17.318102282486574,
                    MinorAxisLength = 12.948852627933807,
                    AxisAngle = 1.5707963267948966
                }
            };

            // Convert the data to dictionaries
            minValues = new Dictionary<string, double>
            {
                { "AspectRatio", data.MinValues.AspectRatio },
                { "CentroidX", data.MinValues.CentroidX },
                { "CentroidY", data.MinValues.CentroidY },
                { "MassToTotalArea", data.MinValues.MassToTotalArea },
                { "IntersectionArrays", data.MinValues.IntersectionArrays },
                { "NegativeSpacesCount", data.MinValues.NegativeSpacesCount },
                { "NegativeSpaceBordersCount", data.MinValues.NegativeSpaceBordersCount },
                { "TotalLengthToDiagonalLengthRatio", data.MinValues.TotalLengthToDiagonalLengthRatio },
                { "TotalNumberOfLineSegments", data.MinValues.TotalNumberOfLineSegments },
                { "AngleChanges", data.MinValues.AngleChanges },
                { "StartPosition", data.MinValues.StartPosition },
                { "EndPosition", data.MinValues.EndPosition },
                { "Circularity", data.MinValues.Circularity },
                { "CentroidPositionX", data.MinValues.CentroidPositionX },
                { "CentroidPositionY", data.MinValues.CentroidPositionY },
                { "RelativeMass", data.MinValues.RelativeMass },
                { "RelativeMax", data.MinValues.RelativeMax },
                { "MajorAxisLength", data.MinValues.MajorAxisLength },
                { "MinorAxisLength", data.MinValues.MinorAxisLength },
                { "AxisAngle", data.MinValues.AxisAngle }
            };

            maxValues = new Dictionary<string, double>
            {
                { "AspectRatio", data.MaxValues.AspectRatio },
                { "CentroidX", data.MaxValues.CentroidX },
                { "CentroidY", data.MaxValues.CentroidY },
                { "MassToTotalArea", data.MaxValues.MassToTotalArea },
                { "IntersectionArrays", data.MaxValues.IntersectionArrays },
                { "NegativeSpacesCount", data.MaxValues.NegativeSpacesCount },
                { "NegativeSpaceBordersCount", data.MaxValues.NegativeSpaceBordersCount },
                { "TotalLengthToDiagonalLengthRatio", data.MaxValues.TotalLengthToDiagonalLengthRatio },
                { "TotalNumberOfLineSegments", data.MaxValues.TotalNumberOfLineSegments },
                { "AngleChanges", data.MaxValues.AngleChanges },
                { "StartPosition", data.MaxValues.StartPosition },
                { "EndPosition", data.MaxValues.EndPosition },
                { "Circularity", data.MaxValues.Circularity },
                { "CentroidPositionX", data.MaxValues.CentroidPositionX },
                { "CentroidPositionY", data.MaxValues.CentroidPositionY },
                { "RelativeMass", data.MaxValues.RelativeMass },
                { "RelativeMax", data.MaxValues.RelativeMax },
                { "MajorAxisLength", data.MaxValues.MajorAxisLength },
                { "MinorAxisLength", data.MaxValues.MinorAxisLength },
                { "AxisAngle", data.MaxValues.AxisAngle }
            };
        }

        public void ExportMinMaxValues(string filePath)
        {
            var minMaxValues = new
            {
                MinValues = minValues,
                MaxValues = maxValues
            };

            string json = JsonConvert.SerializeObject(minMaxValues, Formatting.Indented);

            File.WriteAllText(filePath, json);
        }

        public void UpdateMinMax(OcrFeatures features)
        {
            // Update min and max values for each feature
            UpdateMinMax("AspectRatio", features.AspectRatio);
            UpdateMinMax("CentroidX", features.CentroidX);
            UpdateMinMax("CentroidY", features.CentroidY);
            UpdateMinMax("MassToTotalArea", features.MassToTotalArea);
            UpdateMinMax("MajorAxisLength", features.MaxAxis);
            UpdateMinMax("MinorAxisLength", features.MinAxis);
            UpdateMinMax("AxisAngle", features.AxisAngle);

            // Update min and max values for IntersectionArrays (if needed)
            if (features.IntersectionArrays != null)
            {
                foreach (var list in features.IntersectionArrays)
                {
                    foreach (var value in list)
                    {
                        UpdateMinMax("IntersectionArrays", value);
                    }
                }
            }

            // Update min and max values for NegativeSpaces (if needed)
            if (features.NegativeSpaces != null)
            {
                foreach (var sectionMetrics in features.NegativeSpaces)
                {
                    UpdateMinMax("Circularity", sectionMetrics.Circularity);
                    UpdateMinMax("CentroidPositionX", sectionMetrics.CentroidPositionX);
                    UpdateMinMax("CentroidPositionY", sectionMetrics.CentroidPositionY);
                    UpdateMinMax("RelativeMass", sectionMetrics.RelativeMass);
                    UpdateMinMax("RelativeMax", sectionMetrics.RelativeMax);
                    UpdateMinMax("MajorAxisLength", sectionMetrics.MajorAxisLength);
                    UpdateMinMax("MinorAxisLength", sectionMetrics.MinorAxisLength);
                    UpdateMinMax("AxisAngle", sectionMetrics.AxisAngle);
                }
                UpdateMinMax("NegativeSpacesCount", features.NegativeSpaces.Count);
            }

            // Update min and max values for NegativeSpaceBorders (if needed)
            if (features.NegativeSpaceBorders != null)
            {
                foreach (var sectionMetrics in features.NegativeSpaceBorders)
                {
                    UpdateMinMax("Circularity", sectionMetrics.Circularity);
                    UpdateMinMax("CentroidPositionX", sectionMetrics.CentroidPositionX);
                    UpdateMinMax("CentroidPositionY", sectionMetrics.CentroidPositionY);
                    UpdateMinMax("RelativeMass", sectionMetrics.RelativeMass);
                    UpdateMinMax("RelativeMax", sectionMetrics.RelativeMax);
                    UpdateMinMax("MajorAxisLength", sectionMetrics.MajorAxisLength);
                    UpdateMinMax("MinorAxisLength", sectionMetrics.MinorAxisLength);
                    UpdateMinMax("AxisAngle", sectionMetrics.AxisAngle);
                }
                UpdateMinMax("NegativeSpaceBordersCount", features.NegativeSpaceBorders.Count);
            }

            // Update min and max values for LongestShortestPath (if needed)
            if (features.LongestShortestPath != null)
            {
                UpdateMinMax("TotalLengthToDiagonalLengthRatio", features.LongestShortestPath.TotalLengthToDiagonalLengthRatio);
                UpdateMinMax("TotalNumberOfLineSegments", features.LongestShortestPath.TotalNumberOfLineSegments);
                UpdateMinMax("AspectRatio", features.LongestShortestPath.AspectRatio);

                if (features.LongestShortestPath.AngleChanges != null)
                {
                    foreach (var value in features.LongestShortestPath.AngleChanges)
                    {
                        UpdateMinMax("AngleChanges", value);
                    }
                }

                UpdateMinMax("StartPosition", positionMapping.ContainsKey(features.LongestShortestPath.StartPosition) ? positionMapping[features.LongestShortestPath.StartPosition] : 0);
                UpdateMinMax("EndPosition", positionMapping.ContainsKey(features.LongestShortestPath.EndPosition) ? positionMapping[features.LongestShortestPath.EndPosition] : 0);
            }
        }

        private void UpdateMinMax(string featureName, double value)
        {
            if (!minValues.ContainsKey(featureName))
            {
                minValues[featureName] = value;
                maxValues[featureName] = value;
            }
            else
            {
                minValues[featureName] = Math.Min(minValues[featureName], value);
                maxValues[featureName] = Math.Max(maxValues[featureName], value);
            }
        }

        public List<double> ScaleFeatures(OcrFeatures features)
        {
            var scaledFeatureList = new List<double>
            {
                // Scale simple features
                ScaleFeature("AspectRatio", features.AspectRatio),
                ScaleFeature("CentroidX", features.CentroidX),
                ScaleFeature("CentroidY", features.CentroidY),
                ScaleFeature("MassToTotalArea", features.MassToTotalArea),
                ScaleFeature("MajorAxisLength", features.MaxAxis),
                ScaleFeature("MinorAxisLength", features.MinAxis),
                ScaleFeature("AxisAngle", features.AxisAngle)
            };

            // Scale complex features like IntersectionArrays
            if (features.IntersectionArrays != null)
            {
                foreach (var list in features.IntersectionArrays)
                {
                    scaledFeatureList.AddRange(list.Select(value => value == -1 ? -1 : ScaleFeature("IntersectionArrays", value)));
                }
            }

            // Scale NegativeSpaces and NegativeSpaceBorders
            if (features.NegativeSpaces != null)
            {
                int maxNegativeSpacesCount = (int)maxValues["NegativeSpacesCount"];
                scaledFeatureList.Add(ScaleFeature("NegativeSpacesCount", Math.Min(maxNegativeSpacesCount, features.NumberOfNegativeSpaces)));
                foreach (var sectionMetrics in features.NegativeSpaces.Take(maxNegativeSpacesCount))
                {
                    scaledFeatureList.Add(ScaleFeature("Circularity", sectionMetrics.Circularity));
                    scaledFeatureList.Add(ScaleFeature("AspectRatio", sectionMetrics.AspectRatio));
                    scaledFeatureList.Add(ScaleFeature("CentroidPositionX", sectionMetrics.CentroidPositionX));
                    scaledFeatureList.Add(ScaleFeature("CentroidPositionY", sectionMetrics.CentroidPositionY));
                    scaledFeatureList.Add(ScaleFeature("RelativeMass", sectionMetrics.RelativeMass));
                    scaledFeatureList.Add(ScaleFeature("RelativeMax", sectionMetrics.RelativeMax));
                    scaledFeatureList.Add(ScaleFeature("MajorAxisLength", sectionMetrics.MajorAxisLength));
                    scaledFeatureList.Add(ScaleFeature("MinorAxisLength", sectionMetrics.MinorAxisLength));
                    scaledFeatureList.Add(ScaleFeature("AxisAngle", sectionMetrics.AxisAngle));
                    scaledFeatureList.Add(sectionMetrics.IsEnclosed ? 1d : 0d);
                }

                for (int i = 0; i < maxNegativeSpacesCount - features.NegativeSpaces.Count; i++)
                {
                    for (int j = 0; j < 10; ++j)
                        scaledFeatureList.Add(-1.0);
                }
            }

            if (features.NegativeSpaceBorders != null)
            {
                int maxNegativeSpaceBordersCount = (int)maxValues["NegativeSpaceBordersCount"];
                scaledFeatureList.Add(ScaleFeature("NegativeSpaceBordersCount", Math.Min(maxNegativeSpaceBordersCount, features.NumberOfNegativeSpaceBorders)));
                foreach (var sectionMetrics in features.NegativeSpaceBorders.Take(maxNegativeSpaceBordersCount))
                {
                    scaledFeatureList.Add(ScaleFeature("Circularity", sectionMetrics.Circularity));
                    scaledFeatureList.Add(ScaleFeature("AspectRatio", sectionMetrics.AspectRatio));
                    scaledFeatureList.Add(ScaleFeature("CentroidPositionX", sectionMetrics.CentroidPositionX));
                    scaledFeatureList.Add(ScaleFeature("CentroidPositionY", sectionMetrics.CentroidPositionY));
                    scaledFeatureList.Add(ScaleFeature("RelativeMass", sectionMetrics.RelativeMass));
                    scaledFeatureList.Add(ScaleFeature("RelativeMax", sectionMetrics.RelativeMax));
                    scaledFeatureList.Add(ScaleFeature("MajorAxisLength", sectionMetrics.MajorAxisLength));
                    scaledFeatureList.Add(ScaleFeature("MinorAxisLength", sectionMetrics.MinorAxisLength));
                    scaledFeatureList.Add(ScaleFeature("AxisAngle", sectionMetrics.AxisAngle));
                }

                for (int i = 0; i < maxNegativeSpaceBordersCount - features.NegativeSpaceBorders.Count; i++)
                {
                    for (int j = 0; j < 9; ++j)
                        scaledFeatureList.Add(-1.0);
                }
            }

            // Scale LongestShortestPath features
            if (features.LongestShortestPath != null)
            {
                scaledFeatureList.Add(ScaleFeature("TotalLengthToDiagonalLengthRatio", features.LongestShortestPath.TotalLengthToDiagonalLengthRatio));
                scaledFeatureList.Add(ScaleFeature("TotalNumberOfLineSegments", features.LongestShortestPath.TotalNumberOfLineSegments));
                scaledFeatureList.Add(ScaleFeature("AspectRatio", features.LongestShortestPath.AspectRatio));

                if (features.LongestShortestPath.AngleChanges != null)
                {
                    scaledFeatureList.AddRange(features.LongestShortestPath.AngleChanges.Select(value => ScaleFeature("AngleChanges", value)));
                }

                // Scale StartPosition and EndPosition
                scaledFeatureList.Add(ScaleFeature("StartPosition", positionMapping.ContainsKey(features.LongestShortestPath.StartPosition) ? positionMapping[features.LongestShortestPath.StartPosition] : 0));
                scaledFeatureList.Add(ScaleFeature("EndPosition", positionMapping.ContainsKey(features.LongestShortestPath.EndPosition) ? positionMapping[features.LongestShortestPath.EndPosition] : 0));
            }

            return scaledFeatureList;
        }


        private double ScaleFeature(string featureName, double value)
        {
            if (!minValues.ContainsKey(featureName) || !maxValues.ContainsKey(featureName))
            {
                throw new InvalidOperationException($"Min and max values for feature '{featureName}' are not available.");
            }

            if (value == -1d)
            {

            }

            double min = minValues[featureName];
            double max = maxValues[featureName];

            if (max == min)
            {
                // Handle the case where min and max are the same (constant feature)
                return 0.0;
            }

            // Perform min-max scaling
            return (value - min) / (max - min);
        }
    }

}
