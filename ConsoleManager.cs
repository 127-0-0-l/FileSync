using System.Text;

namespace FileSync
{
    internal static class ConsoleManager
    {
        private static int _consoleWidth;
        private static int _progressBarMaxWidth = 100;

        public static void RewriteLines(string[] lines)
        {
            if (lines.Length < 1)
                return;

            CheckResize();

            try
            {
                Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop - (lines.Length - 1)));

                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = MakeConsoleRow(lines[i]);

                    if (i == lines.Length - 1)
                        Console.Write(lines[i]);
                    else
                        Console.WriteLine(lines[i]);
                }
            }
            catch { }
        }

        public static void RewriteLinesWithProgress(string[] lines, int percentage)
        {
            if (lines.Length < 1 || percentage < 0 || percentage > 100)
                return;

            CheckResize();

            try
            {
                Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop - lines.Length));

                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = MakeConsoleRow(lines[i]);
                    Console.WriteLine(lines[i]);
                }

                Console.Write(GetProgressBar(percentage));
            }
            catch { }
        }

        private static string GetProgressBar(int percentage)
        {
            string begining = "[";
            string ending = $"] {percentage}%";

            int length = Math.Min(_progressBarMaxWidth, _consoleWidth);
            int pbLength = length - begining.Length - ending.Length;

            StringBuilder result = new StringBuilder();
            result.Append(begining);

            int filledCells = (pbLength * percentage) / 100;
            for (int i = 0; i < filledCells; i++)
                result.Append('#');
            for (int i = 0; i < pbLength - filledCells; i++)
                result.Append(' ');

            result.Append(ending);

            return result.ToString();
        }

        private static string MakeConsoleRow(string str)
        {
            char[] newStr = new char[_consoleWidth];
            for (int i = 0; i < _consoleWidth; i++)
            {
                if (i < str.Length)
                    newStr[i] = (i >= _consoleWidth - 3) && (str.Length > _consoleWidth) ? '.' : str[i];
                else
                    newStr[i] = ' ';
            }

            return new string(newStr);
        }

        private static void CheckResize()
        {
            int width = Console.WindowWidth;

            if (_consoleWidth != width)
                _consoleWidth = width;
        }
    }
}
