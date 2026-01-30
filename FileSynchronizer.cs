using Serilog;

namespace FileSync
{
    internal class FileSynchronizer
    {
        private readonly DirectoryInfo _sourceDirectory;
        private readonly DirectoryInfo _destinationDirectory;
        private Queue<DiskTask> _diskTasks = new Queue<DiskTask>();
        private List<string> _failedToScan = new List<string>();
        private List<string> _failedToDelete = new List<string>();
        private List<string> _failedToCopy = new List<string>();
        private long _commonSize;

        public FileSynchronizer(string sourcePath, string destinationPath)
        {
            _sourceDirectory = new DirectoryInfo(sourcePath);
            _destinationDirectory = new DirectoryInfo(destinationPath);
            _commonSize = 0;
            Log.Information($"create FileSynchronizer(sourcePath: {sourcePath}, destinationPath: {destinationPath})");
        }

        public void Synchronize()
        {
            Log.Information($"start synchronizing {_sourceDirectory} to {_destinationDirectory}");

            if (!IsPathsValid())
                throw new Exception("invalid path");

            Console.WriteLine("scanning...");
            ScanDirectories(_sourceDirectory, _destinationDirectory);

            int dirToDelete = _diskTasks.Count(t => !t.IsFile && t.TaskType == DiskTaskType.Delete);
            int filesToDelete = _diskTasks.Count(t => t.IsFile && t.TaskType == DiskTaskType.Delete);
            int filesToCopy = _diskTasks.Count(t => t.IsFile && t.TaskType == DiskTaskType.Copy);

            ConsoleManager.RewriteLines(new string[]
            {
                "scanning complete",
                $"{dirToDelete} directories {filesToDelete} files to delete"
            });
            Console.WriteLine($"\n{filesToCopy} filesToCopy");

            Console.WriteLine("\n\nsyncing...");
            SyncDirectories();

            if (_failedToScan.Count > 0)
            {
                Console.WriteLine("\nfailed to scan:");
                foreach (var item in _failedToScan)
                    Console.WriteLine(item);
            }

            if (_failedToDelete.Count > 0)
            {
                Console.WriteLine("\nfailed to delete:");
                foreach (var item in _failedToDelete)
                    Console.WriteLine(item);
            }

            if (_failedToCopy.Count > 0)
            {
                Console.WriteLine("\nfailed to copy:");
                foreach (var item in _failedToCopy)
                    Console.WriteLine(item);
            }
        }

        private void ScanDirectories(DirectoryInfo source, DirectoryInfo destination)
        {
            Log.Information($"scan {source} -> {destination}");

            try
            {
                ConsoleManager.RewriteLines(new string[] { source.FullName });

                // delete directories
                foreach (var directory in destination.GetDirectories())
                {
                    if (!Directory.Exists(Path.Combine(source.FullName, directory.Name)))
                    {
                        EnqueueDelete(directory.FullName, false);
                        _commonSize++;
                    }
                }

                // create directories
                foreach (var directory in source.GetDirectories())
                {
                    DirectoryInfo destDirectory = new DirectoryInfo(Path.Combine(destination.FullName, directory.Name));
                    if (!destDirectory.Exists)
                        destDirectory.Create();

                    ScanDirectories(directory, destDirectory);
                }

                // delete files
                foreach (var file in destination.GetFiles())
                {
                    FileInfo sourceFile = new FileInfo(Path.Combine(source.FullName, file.Name));
                    if (!sourceFile.Exists)
                    {
                        EnqueueDelete(file.FullName, true);
                        _commonSize++;
                    }
                    else if (sourceFile.LastWriteTimeUtc != file.LastWriteTimeUtc)
                    {
                        EnqueueDelete(file.FullName, true);
                        _commonSize++;
                        EnqueueCopy(sourceFile, file);
                        _commonSize += sourceFile.Length;
                    }
                }

                // copy files
                foreach (var file in source.GetFiles())
                {
                    FileInfo destFile = new FileInfo(Path.Combine(destination.FullName, file.Name));
                    if (!destFile.Exists)
                    {
                        EnqueueCopy(file, destFile);
                        _commonSize += file.Length;
                    }
                }
            }
            catch
            {
                string directories = $"(source: {source}) (destination: {destination})";
                _failedToScan.Add(directories);
                Log.Information("failed to scan " + directories);
            }
        }

        private void SyncDirectories()
        {
            long processed = 0;

            int dtCount = _diskTasks.Count;
            for (int i = 0; i < dtCount; i++)
            {
                DiskTask item = _diskTasks.Dequeue();
                int percantage = (int)((processed * 100) / _commonSize);

                Log.Information($"{item.TaskType} {item.SourcePath} -> {item.DestinationPath}");

                switch (item.TaskType)
                {
                    case DiskTaskType.Delete:
                        ConsoleManager.RewriteLinesWithProgress(
                            new string[] { $"delete {item.DestinationPath}", GetProgressInfoString(processed) },
                            percantage);

                        try
                        {
                            if (item.IsFile)
                                File.Delete(item.DestinationPath);
                            else
                                Directory.Delete(item.DestinationPath, true);
                        }
                        catch
                        {
                            try
                            {
                                // work only on Windows
                                ForceDelete(item.DestinationPath, item.IsFile);
                            }
                            catch
                            {
                                _failedToDelete.Add(item.DestinationPath);
                                Log.Information($"failed to delete {item.DestinationPath}");
                            }
                        }

                        processed++;
                        break;

                    case DiskTaskType.Copy:
                        Log.Information($"copy {item.SourcePath}");
                        ConsoleManager.RewriteLinesWithProgress(
                            new string[] { $"copy {item.SourcePath}", GetProgressInfoString(processed) },
                            percantage);

                        if (item.IsFile)
                            try
                            {
                                using (FileStream sourceStream = new FileStream(item.SourcePath, FileMode.Open, FileAccess.Read))
                                using (FileStream destinationStream = new FileStream(item.DestinationPath, FileMode.Create, FileAccess.Write))
                                {
                                    int kb = 4;
                                    byte[] buffer = new byte[1024 * kb];
                                    int bytesRead;

                                    int counter = 0;
                                    int counterMax = 1024 / kb;
                                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        destinationStream.Write(buffer, 0, bytesRead);
                                        processed += bytesRead;
                                        counter++;
                                        if (counter == counterMax)
                                        {
                                            percantage = (int)((processed * 100) / _commonSize);
                                            ConsoleManager.RewriteLinesWithProgress(
                                                new string[] { $"copy {item.SourcePath}", GetProgressInfoString(processed) },
                                                percantage);
                                            counter = 0;
                                        }
                                    }
                                }

                                File.SetLastWriteTimeUtc(item.DestinationPath, File.GetLastWriteTimeUtc(item.SourcePath));
                                File.SetCreationTimeUtc(item.DestinationPath, File.GetCreationTimeUtc(item.SourcePath));
                            }
                            catch
                            {
                                _failedToCopy.Add(item.SourcePath);
                                Log.Information($"failed to copy {item.SourcePath}");
                            }
                        break;
                    default:
                        break;
                }
            }

            Log.Information("syncing complete");
            ConsoleManager.RewriteLinesWithProgress(new string[] { "", "syncing complete" }, 100);
            Console.WriteLine();
        }

        // work only on Windows
        private void ForceDelete(string path, bool isFile = false)
        {
            if (isFile)
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
            else
            {
                foreach (var dir in Directory.GetDirectories(path))
                    if (dir != null)
                        ForceDelete(dir);

                foreach (var file in Directory.GetFiles(path))
                    ForceDelete(file, true);

                Directory.Delete(path);
            }
        }

        private bool IsPathsValid() => _sourceDirectory.Exists && _destinationDirectory.Exists;

        private string GetProgressInfoString(long processed) =>
            $"{processed / 1024 / 1024} / {_commonSize / 1024 / 1024} MB";

        private void EnqueueDelete(string path, bool isFile)
        {
            _diskTasks.Enqueue(new DiskTask
            {
                TaskType = DiskTaskType.Delete,
                DestinationPath = path,
                IsFile = isFile
            });
        }

        private void EnqueueCopy(FileInfo source, FileInfo destination)
        {
            _diskTasks.Enqueue(new DiskTask
            {
                TaskType = DiskTaskType.Copy,
                SourcePath = source.FullName,
                DestinationPath = destination.FullName,
                IsFile = true
            });
        }
    }
}
