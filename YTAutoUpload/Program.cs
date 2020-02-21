using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTAutoUpload
{
    class Program
    {
        public static void Main(string[] args)
        {
            int length = (int)Ffmpeg.GetLength("test.mp4");
            Ffmpeg.Cut("test.mp4", "out/1.mp4", 0, false, length / 2);
            Ffmpeg.Cut("test.mp4", "out/2.mp4", length / 2, false);
            Ffmpeg.BuildList("out/list.txt", "1.mp4", "2.mp4");
            Ffmpeg.Concat("out/list.txt", "out/concat.mp4");
            Console.ReadKey();
        }


    }
}
