using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTAutoUpload
{
    public class FfProcess : IDisposable
    {
        Process process;
        LinkedList<string> stdout = new LinkedList<string>();
        LinkedList<string> stderr = new LinkedList<string>();

        public FfProcess(string filename, string args)
        {
            process = new Process();
            process.StartInfo.FileName = filename;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputErrHandler);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        public string ReadLine()
        {
            string result = stdout.First.Value;
            stdout.RemoveFirst();
            return result;
        }

        public string ReadErrLine()
        {
            string result = stderr.First.Value;
            stdout.RemoveFirst();
            return result;
        }

        public void WaitForExit()
        {
            process.WaitForExit();
        }

        public void Dispose()
        {
            process.Dispose();
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            stdout.AddLast(outLine.Data);
            //Console.WriteLine(outLine.Data);
        }

        private void OutputErrHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            stderr.AddLast(outLine.Data);
            //Console.WriteLine(outLine.Data);
        }
    }
}
