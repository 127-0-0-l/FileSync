namespace FileSync
{
    internal class FileSynchronizer
    {
        private readonly DirectoryInfo _sourceDirectory;
        private readonly DirectoryInfo _destinationDirectory;
        private Queue<DiskTask> _diskTasks = new Queue<DiskTask>();
        private List<string> _failedToDelete = new List<string>();
        private List<string> _failedToCopy = new List<string>();

        public FileSynchronizer(string sourcePath, string destinationPath)
        {
            _sourceDirectory = new DirectoryInfo(sourcePath);
            _destinationDirectory = new DirectoryInfo(destinationPath);
        }

        public void Synchronize()
        {
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

            Console.WriteLine("\nsyncing...");
            SyncDirectories();

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
            ConsoleManager.RewriteLines(new string[] { source.FullName });

            // delete directories
            foreach (var directory in destination.GetDirectories())
            {
                if (!Directory.Exists(Path.Combine(source.FullName, directory.Name)))
                {
                    _diskTasks.Enqueue(new DiskTask
                    {
                        TaskType = DiskTaskType.Delete,
                        DestinationPath = directory.FullName
                    });
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
                if (!sourceFile.Exists || sourceFile.LastWriteTimeUtc != file.LastWriteTimeUtc)
                {
                    _diskTasks.Enqueue(new DiskTask
                    {
                        TaskType = DiskTaskType.Delete,
                        DestinationPath = file.FullName,
                        IsFile = true
                    });
                }
            }

            // copy files
            foreach (var file in source.GetFiles())
            {
                FileInfo destFile = new FileInfo(Path.Combine(destination.FullName, file.Name));
                if (!destFile.Exists)
                {
                    _diskTasks.Enqueue(new DiskTask
                    {
                        TaskType = DiskTaskType.Copy,
                        SourcePath = file.FullName,
                        DestinationPath = destFile.FullName,
                        IsFile = true
                    });
                }
            }
        }

        private void SyncDirectories()
        {
            int dtCount = _diskTasks.Count;
            for (int i = 0; i < dtCount; i++)
            {
                int percantage = (i * 100) / dtCount;
                DiskTask item = _diskTasks.Dequeue();

                switch (item.TaskType)
                {
                    case DiskTaskType.Delete:
                        ConsoleManager.RewriteLinesWithProgress(
                            new string[] {$"delete {item.DestinationPath}"}, percantage);

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
                            }
                        }
                        break;
                    case DiskTaskType.Copy:
                        ConsoleManager.RewriteLinesWithProgress(
                            new string[] { $"copy {item.SourcePath}" }, percantage);

                        if (item.IsFile)
                            try
                            {
                                File.Copy(item.SourcePath, item.DestinationPath);
                            }
                            catch
                            {
                                _failedToCopy.Add(item.SourcePath);
                            }
                        break;
                    default:
                        break;
                }
            }

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
    }
}
