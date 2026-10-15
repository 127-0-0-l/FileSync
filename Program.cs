using FileSync.Interfaces;
using FileSync.Services;
using Serilog;

namespace FileSync
{
    public class Program
    {
        static async Task Main(string[] args)
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

                    IOutputService outputService =
                        new SynchroniusOutputServiceDecorator(
                            new LoggingOutputServiceDecorator(
                                new ConsoleOutputService()));

                    TaskQueue.Start();
                    FileSynchronizer fs = new FileSynchronizer(outputService, sourcePath, destinationPath);
                    fs.Synchronize();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Log.Error(ex.Message);
                }
                finally
                {
                    await TaskQueue.StopAsync();
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
