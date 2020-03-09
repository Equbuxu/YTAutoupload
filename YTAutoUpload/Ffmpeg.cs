using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTAutoUpload
{
    public class Ffmpeg
    {
        public static double GetLength(string video)
        {
            double length;
            if (!File.Exists(video))
                return -1;
            using (FfProcess process = new FfProcess("ffprobe.exe", $"-i {video} -show_entries format=duration -v quiet -of csv=\"p = 0"))
            {
                process.WaitForExit();
                length = double.Parse(process.ReadLine(), CultureInfo.InvariantCulture);
            }
            return length;
        }

        public static void Cut(string video, string output, int start, bool fast, int duration = -1)
        {
            if (!File.Exists(video))
                return;
            string args = $"-i \"{video}\" -ss {start} ";
            if (duration >= 0)
                args += $"-t {duration} ";
            if (fast)
                args += "-c copy ";
            args += $"-async 1 -y \"{output}\"";
            using (FfProcess process = new FfProcess("ffmpeg.exe", args))
            {
                process.WaitForExit();
            }
        }

        public static void BuildList(string output, List<string> videos)
        {
            using (StreamWriter writer = new StreamWriter(File.Create(output)))
            {
                foreach (string video in videos)
                {
                    writer.WriteLine($"file '{video}'");
                }
            }
        }

        public static void Concat(string list, string output)
        {
            string args = $"-f concat -safe 0 -i \"{list}\" -c copy -y \"{output}\"";
            using (FfProcess process = new FfProcess("ffmpeg.exe", args))
            {
                process.WaitForExit();
            }
        }

        public static void RemoveInactiveSegments(string input, string output)
        {
            string args = $"-i {input} -vf \"select=gt(scene\\,0.00001),setpts=N/(60*TB)\" -y {output}";
            using (FfProcess process = new FfProcess("ffmpeg.exe", args))
            {
                process.WaitForExit();
            }
        }
    }
}
