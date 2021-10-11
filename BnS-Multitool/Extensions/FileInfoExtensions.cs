namespace System.IO
{
    public static class FileInfoExtensions
    {
        public static void Rename(this FileInfo fileInfo, string newName)
        {
            if (!fileInfo.Exists)
                return;

            if (File.Exists(fileInfo.Directory + @"\" + newName))
                File.Delete(fileInfo.Directory + @"\" + newName);

            fileInfo.MoveTo(Path.Combine(fileInfo.Directory.FullName, newName));
        }
    }
}