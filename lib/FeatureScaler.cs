using ImageProcess.Model;

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
            minValues = new Dictionary<string, double>();
            maxValues = new Dictionary<string, double>();
        }

        public void UpdateMinMax(OcrFeatures features)
        {
            // Update min and max values for each feature
            UpdateMinMax("AspectRatio", features.AspectRatio);
            UpdateMinMax("CentroidX", features.CentroidX);
            UpdateMinMax("CentroidY", features.CentroidY);
            UpdateMinMax("MassToTotalArea", features.MassToTotalArea);

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
            }

            // Update min and max values for LongestShortestPath (if needed)
            if (features.LongestShortestPath != null)
            {
                UpdateMinMax("TotalLengthToDiagonalLengthRatio", features.LongestShortestPath.TotalLengthToDiagonalLengthRatio);
                UpdateMinMax("TotalNumberOfLineSegments", features.LongestShortestPath.TotalNumberOfLineSegments);

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
                ScaleFeature("MassToTotalArea", features.MassToTotalArea)
            };
            // ... add other simple features ...

            // Scale complex features like IntersectionArrays
            if (features.IntersectionArrays != null)
            {
                foreach (var list in features.IntersectionArrays)
                {
                    scaledFeatureList.AddRange(list.Select(value => ScaleFeature("IntersectionArrays", value)));
                }
            }

            // Scale NegativeSpaces and NegativeSpaceBorders
            scaledFeatureList.Add(ScaleFeature("NumberOfNegativeSpaces", features.NumberOfNegativeSpaces));
            if (features.NegativeSpaces != null)
            {
                foreach (var sectionMetrics in features.NegativeSpaces)
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
            }

            scaledFeatureList.Add(ScaleFeature("NumberOfNegativeSpaceBorders", features.NumberOfNegativeSpaceBorders));
            if (features.NegativeSpaceBorders != null)
            {
                foreach (var sectionMetrics in features.NegativeSpaceBorders)
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
            }

            // Scale LongestShortestPath features
            if (features.LongestShortestPath != null)
            {
                scaledFeatureList.Add(ScaleFeature("TotalLengthToDiagonalLengthRatio", features.LongestShortestPath.TotalLengthToDiagonalLengthRatio));
                scaledFeatureList.Add(ScaleFeature("TotalNumberOfLineSegments", features.LongestShortestPath.TotalNumberOfLineSegments));

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
