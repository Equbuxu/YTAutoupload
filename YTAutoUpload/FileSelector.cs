using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTAutoUpload
{
    public class FileSelector
    {
        public static List<string> SelectFiles(string directoryPath, DateTime from, DateTime to)
        {
            if (from >= to)
                return new List<string>();

            string[] allFiles = Directory.GetFiles(directoryPath);
            Dictionary<DateTime, string> files = new Dictionary<DateTime, string>();
            List<DateTime> sortedFiles = new List<DateTime>();

            foreach (string file in allFiles)
            {
                DateTime? time = ParseTimestamp(file);
                if (!time.HasValue)
                    continue;
                files.Add(time.Value, file);
                sortedFiles.Add(time.Value);
            }
            sortedFiles.Sort();

            //find newest file inside datetime range
            int newest = -1; ;
            for (int i = sortedFiles.Count -1; i >= 0; i--)
            {
                if (sortedFiles[i] < to)
                {
                    newest = i;
                    break;
                }
            }
            if (newest == -1)
                return new List<string>();

            //choose all files inside range
            List<string> selectedFiles = new List<string>();
            for (int i = newest; i >= 0; i--)
            {
                selectedFiles.Add(files[sortedFiles[i]]);
                if (sortedFiles[i] < from)
                    break;
            }
            selectedFiles.Reverse();
            return selectedFiles;
        }

        public static double ParseSpeedMult(string path)
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            string[] spl = filename.Split('_');
            if (spl.Length != 3)
                return -1;
            double result;
            if (!double.TryParse(spl[2].Replace("x", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                return -1;
            return result;
        }

        public static DateTime? ParseTimestamp(string path)
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            //example: 2020-02-21_13-53-14_20.08x
            string[] spl = filename.Split('_');
            if (spl.Length != 3)
                return null;
            string[] date = spl[0].Split('-');
            string[] time = spl[1].Split('-');
            if (date.Length != 3 || time.Length != 3)
                return null;

            try
            {
                int year = int.Parse(date[0]);
                int month = int.Parse(date[1]);
                int day = int.Parse(date[2]);

                int hour = int.Parse(time[0]);
                int minute = int.Parse(time[1]);
                int second = int.Parse(time[2]);
                return new DateTime(year, month, day, hour, minute, second);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
