using FileSync.Interfaces;

namespace FileSync.Services
{
    public class OutputServiceDecorator : IOutputService
    {
        private readonly IOutputService _outputService;

        public OutputServiceDecorator(IOutputService outputService) =>
            _outputService = outputService;

        public virtual void Write(string text) => _outputService.Write(text);

        public virtual void WriteLine() => _outputService.WriteLine();

        public virtual void WriteLine(string line) => _outputService.WriteLine(line);

        public virtual void WriteLines(string[] lines) => _outputService.WriteLines(lines);

        public virtual void RewriteLines(string[] lines) => _outputService.RewriteLines(lines);

        public virtual void RewriteLinesWithProgress(string[] lines, int percentage) =>
            _outputService.RewriteLinesWithProgress(lines, percentage);
    }
}
