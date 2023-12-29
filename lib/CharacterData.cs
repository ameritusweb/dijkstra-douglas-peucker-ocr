using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    public class CharacterData
    {
        public List<Angle> AngleList { get; set; }

        public (double X, double Y, int Z) CenterOfMassOffset { get; set; }

        public Dictionary<List<int>, decimal> Percentages { get; set; }

        public double AspectRatio { get; set; }

        public static decimal MinValue { get; set; }

        public static decimal MaxValue { get; set; }

        public List<decimal> Normalize()
        {
            List<decimal> decimals = new List<decimal>();
            foreach (var angle in AngleList)
            {
                decimals.Add(angle.angle * 1.0m * (1/360m));
                var i = angle.intersections;
                if (i.Count > 10)
                {

                }
                PadListToSize(i, 15, -1);
                i.ForEach(x => decimals.Add(x));
            }
            foreach (var kvp in Percentages)
            {
                var key = kvp.Key;
                PadListToSize(key, 15, -1);
                key.ForEach(x => decimals.Add(x));
                decimals.Add(kvp.Value * 1m / 50m);
            }
            decimals.Add((decimal)CenterOfMassOffset.X);
            decimals.Add((decimal)CenterOfMassOffset.Y);
            decimals.Add((decimal)AspectRatio);
            return Scale(decimals);
        }

        public static List<decimal> Scale(List<decimal> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Count == 0)
                throw new InvalidOperationException("Cannot scale an empty list.");

            decimal min = -3m;//data.Min();
            decimal max = 5m;//data.Max();
            // if (min < MinValue)
            //   MinValue = min;
            //if (max > MaxValue)
            //    MaxValue = max;

            // If all values are the same, return a list of 0.5 to avoid division by zero
            if (min == max || data.Min() == data.Max())
                return data.Select(_ => 0.5m).ToList();

            var res = data.Select(x => x == -1 ? 0 : (x - min) / (max - min)).ToList();
            return res;
        }

        public static void PadListToSize(List<int> list, int targetSize, int padValue)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count > targetSize)
            {

            }

            int paddingNeeded = targetSize - list.Count;
            for (int i = 0; i < paddingNeeded; i++)
            {
                list.Add(padValue);
            }
        }
    }
}
