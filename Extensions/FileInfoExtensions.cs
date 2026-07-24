using Blake3;

namespace FileSync.Extensions
{
    public static class FileInfoExtensions
    {
        public static Hash GetBlake3Hash(this FileInfo file)
        {
            if (!file.Exists)
                throw new FileNotFoundException();

            var hasher = Blake3.Hasher.New();
            byte[] buffer = new byte[4096];
            int bytesRead = 0;

            using (FileStream fs = file.OpenRead())
            {
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    hasher.Update(buffer.AsSpan(0, bytesRead));
                }
            }

            return hasher.Finalize();
        }
    }
}
