using FileSync.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSync.Services
{
    public class SynchroniusOutputServiceDecorator : OutputServiceDecorator
    {
        private static readonly object _locker = new();

        public SynchroniusOutputServiceDecorator(IOutputService outputService)
            : base(outputService) { }

        public override void Write(string text)
        {
            lock (_locker)
            {
                base.Write(text);
            }
        }

        public override void WriteLine(string line)
        {
            lock (_locker)
            {
                base.WriteLine(line);
            }
        }

        public override void WriteLines(string[] lines)
        {
            lock (_locker)
            {
                base.WriteLines(lines);
            }
        }

        public override void RewriteLines(string[] lines)
        {
            lock (_locker)
            {
                base.RewriteLines(lines);
            }
        }

        public override void RewriteLinesWithProgress(string[] lines, int percentage)
        {
            lock (_locker)
            {
                base.RewriteLinesWithProgress(lines, percentage);
            }
        }
    }
}
