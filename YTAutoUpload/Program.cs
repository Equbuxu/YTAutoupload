using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTAutoUpload
{
    class Program
    {
        public static void Main(string[] args)
        {
            DateTime from = new DateTime(2020, 1, 5, 3, 0, 0);
            DateTime to = new DateTime(2020, 1, 10, 3, 0, 0);

            CreateVideo("result.mp4", "7/videoout", from, to);
            Console.ReadKey();
        }

        public static void CreateVideo(string output, string canvas, DateTime from, DateTime to)
        {
            List<string> files = FileSelector.SelectFiles(canvas, from, to);
            List<string> finalList = new List<string>();

            CropCalcResult startCropResult;
            int startSeconds = CalculateCropSeconds(files[0], from, out startCropResult);
            if (startCropResult == CropCalcResult.Ok)
            {
                Ffmpeg.Cut(files[0], "start.mp4", startSeconds, true);
                finalList.Add("start.mp4");
            }
            else if (startCropResult == CropCalcResult.VideoLater)
            {
                finalList.Add(files[0]);
            }

            for (int i = 1; i < files.Count - 1; i++)
                finalList.Add(files[i]);

            CropCalcResult endCropResult;
            int endSeconds = CalculateCropSeconds(files[files.Count - 1], to, out endCropResult);
            if (endCropResult == CropCalcResult.Ok)
            {
                Ffmpeg.Cut(files[files.Count - 1], "end.mp4", 0, true, endSeconds);
                finalList.Add("end.mp4");
            }
            else if (endCropResult == CropCalcResult.VideoEarlier)
            {
                finalList.Add(files[files.Count -1]);
            }

            Ffmpeg.BuildList("list.txt", finalList);
            Ffmpeg.Concat("list.txt", output);

            if (File.Exists("start.mp4"))
                File.Delete("start.mp4");
            if (File.Exists("end.mp4"))
                File.Delete("end.mp4");
            //if (File.Exists("list.txt"))
            //    File.Delete("list.txt");
        }

        public enum CropCalcResult
        {
            Ok, VideoEarlier, VideoLater, Error
        }
        public static int CalculateCropSeconds(string file, DateTime intervalBoundary, out CropCalcResult result)
        {
            DateTime? timestamp = FileSelector.ParseTimestamp(file);
            double speedMult = FileSelector.ParseSpeedMult(file);
            if (speedMult == -1 || !timestamp.HasValue)
            {
                result = CropCalcResult.Error;
                return -1;
            }
            if (timestamp > intervalBoundary)
            {
                result = CropCalcResult.VideoLater;
                return -1;
            }
            double duration = Ffmpeg.GetLength(file);
            double realDuration = (int)(duration * speedMult);
            double expectedCropStartSeconds = (intervalBoundary - timestamp.Value).TotalSeconds;
            if (realDuration < expectedCropStartSeconds)
            {
                result = CropCalcResult.VideoEarlier;
                return -1;
            }
            result = CropCalcResult.Ok;
            return (int)(expectedCropStartSeconds / realDuration * duration);
        }
    }
}
