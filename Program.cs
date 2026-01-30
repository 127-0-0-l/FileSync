using Serilog;

namespace FileSync
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                try
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                        .CreateLogger();

                    string sourcePath = args[0];
                    string destinationPath = args[1];

                    FileSynchronizer fs = new FileSynchronizer(sourcePath, destinationPath);
                    fs.Synchronize();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Log.CloseAndFlush();
                }
            }
            else
            {
                string warnMessage = "wrong arguments";
                Log.Warning(warnMessage);
                Console.WriteLine(warnMessage);

            }
        }
    }
}
