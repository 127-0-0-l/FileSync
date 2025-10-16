namespace FileSync
{
    internal class DiskTask
    {
        public DiskTaskType TaskType { get; set; }

        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }

        public bool IsFile { get; set; }
    }
}
