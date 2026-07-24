using FileSync.Interfaces;
using Serilog;

namespace FileSync.Services
{
    public class LoggingOutputServiceDecorator : OutputServiceDecorator
    {
        public LoggingOutputServiceDecorator(IOutputService outputService)
            : base(outputService) { }

        public override void Write(string text)
        {
            LogInfo(text);
            base.Write(text);
        }

        public override void WriteLine(string line)
        {
            LogInfo(line);
            base.WriteLine(line);
        }

        public override void WriteLines(string[] lines)
        {
            LogInfo(lines);
            base.WriteLines(lines);
        }

        public override void RewriteLines(string[] lines)
        {
            LogInfo(lines);
            base.RewriteLines(lines);
        }

        public override void RewriteLinesWithProgress(string[] lines, int percentage)
        {
            LogInfo(lines);
            base.RewriteLinesWithProgress(lines, percentage);
        }

        private void LogInfo(string line) => Log.Information("___program output: {Line}", line);

        private void LogInfo(string[] lines)
        {
            foreach (var line in lines)
                LogInfo(line);
        }
    }
}
