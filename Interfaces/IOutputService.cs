namespace FileSync.Interfaces
{
    public interface IOutputService
    {
        void Write(string text);

        void WriteLine();

        void WriteLine(string line);

        void WriteLines(string[] lines);

        void RewriteLines(string[] lines);

        void RewriteLinesWithProgress(string[] lines, int percentage);
    }
}
