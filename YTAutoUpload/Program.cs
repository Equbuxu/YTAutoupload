using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YTAutoUpload
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(FileSelector.GetLatestRecordingTime("7/videoout"));
            /*
            Youtube youtube = new Youtube();
            youtube.Auth("client_secret.json");
            youtube.UpdateCachedPlaylists();
            //youtube.CreatePlaylist("test autoplaylist", "this is a descriprion", true, new string[] { "rag1", "rag2" });
            //youtube.AddVideoToPlaylist("5Vcq26L9qnw", "PLicc_0TrQcHalaPGBGVImFNGcHnZ2wSjS");

            UploadDailyVideo(new DateTime(2020, 1, 6), 7, youtube);*/

            Console.ReadKey();
        }

        public static void UploadLoop(DateTime startDateTime7, DateTime startDateTime8, DateTime startDateTime9)
        {
            Youtube youtube = new Youtube();
            youtube.Auth("client_secret.json");
            youtube.UpdateCachedPlaylists();

            while (true)
            {
                //7
                DateTime lastRecording7 = FileSelector.GetLatestRecordingTime("7/videoout");
                if (lastRecording7 >= startDateTime7)
                {
                    Console.WriteLine("Uploading video for canvas 7...");
                    Console.WriteLine(UploadDailyVideo(startDateTime7, 7, youtube) ? "Uploaded successfully" : "Failed to upload");
                    startDateTime7 = startDateTime7.AddDays(1);
                }

                //8
                DateTime lastRecording8 = FileSelector.GetLatestRecordingTime("8/videoout");
                if (lastRecording8 >= startDateTime8)
                {
                    Console.WriteLine("Uploading video for canvas 8...");
                    Console.WriteLine(UploadDailyVideo(startDateTime8, 8, youtube) ? "Uploaded successfully" : "Failed to upload");
                    startDateTime8 = startDateTime8.AddDays(1);
                }

                Thread.Sleep(1000 * 60 * 60);
            }
        }

        public static bool UploadDailyVideo(DateTime day, int canvas, Youtube youtube)
        {
            DateTime from = new DateTime(day.Year, day.Month, day.Day, 3, 0, 0);
            DateTime to = from.AddDays(1);

            CreateVideo("result.mp4", canvas + "/videoout", from, to);

            string title = $"{GetShortMonth(day)} {day.Day}, {day.Year} - {GetCanvasName(canvas)} - 24 hours timelapse";

            DateTime uniFrom = from.ToUniversalTime();
            DateTime uniTo = to.ToUniversalTime();
            string description = $"http://pixelplace.io timelapse from {uniFrom.ToShortDateString()} {uniFrom.ToShortTimeString()} UTC to {uniTo.ToShortDateString()} {uniTo.ToShortTimeString()} UTC.";

            string playlistName = $"{GetCanvasName(canvas)} - {GetLongMonth(day)} {day.Year}";

            //Retrieve or create playlist
            string playlistId = null;
            if (youtube.CachedPlaylists.ContainsKey(playlistName))
                playlistId = youtube.CachedPlaylists[playlistName];
            else
            {
                if (youtube.CreatePlaylist(playlistName, "", false, new string[0]))
                    playlistName = youtube.CachedPlaylists[playlistName];
            }

            //Upload video
            string uploadedId;
            if (!youtube.UploadVideo("result.mp4", title, description, new string[] { "pixelplace", "timelapse" }, true, out uploadedId))
                return false;

            //Add video to playlist
            if (playlistId != null)
                youtube.AddVideoToPlaylist(uploadedId, playlistId);

            return true;
        }

        public static string GetCanvasName(int canvasId)
        {
            switch (canvasId)
            {
                case 1:
                    return "CANVAS #1";
                case 2:
                    return "CANVAS #2";
                case 3:
                    return "CANVAS #3";
                case 4:
                    return "r/place continued";
                case 5:
                    return "r/place final state";
                case 6:
                    return "GAMEBOY CANVAS";
                case 7:
                    return "Pixels World War";
                case 8:
                    return "MVP";
                case 9:
                    return "Inverted";
            }
            return $"Canvas {canvasId}";
        }

        public static string GetShortMonth(DateTime date)
        {
            switch (date.Month)
            {
                case 1:
                    return "Jan";
                case 2:
                    return "Feb";
                case 3:
                    return "Mar";
                case 4:
                    return "Apr";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "Aug";
                case 9:
                    return "Sep";
                case 10:
                    return "Oct";
                case 11:
                    return "Nov";
                case 12:
                    return "Dec";
            }
            return null;
        }

        public static string GetLongMonth(DateTime date)
        {
            switch (date.Month)
            {
                case 1:
                    return "January";
                case 2:
                    return "February";
                case 3:
                    return "March";
                case 4:
                    return "April";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "August";
                case 9:
                    return "September";
                case 10:
                    return "October";
                case 11:
                    return "November";
                case 12:
                    return "December";
            }
            return null;
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
                finalList.Add(files[files.Count - 1]);
            }

            Ffmpeg.BuildList("list.txt", finalList);
            Ffmpeg.Concat("list.txt", output);

            if (File.Exists("start.mp4"))
                File.Delete("start.mp4");
            if (File.Exists("end.mp4"))
                File.Delete("end.mp4");
            if (File.Exists("list.txt"))
                File.Delete("list.txt");
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
