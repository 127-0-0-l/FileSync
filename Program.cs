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
                    string sourcePath = args[0];
                    string destinationPath = args[1];

                    FileSynchronizer fs = new FileSynchronizer(sourcePath, destinationPath);
                    fs.Synchronize();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("wrong arguments");
            }
        }
    }
}
