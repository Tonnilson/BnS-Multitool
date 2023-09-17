using BnS_Multitool.Extensions;
using MiscUtil.Compression.Vcdiff;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnS_Multitool.Functions
{
    public class IO
    {
        public static void WriteAllText(string path, string contents)
        {
            // generate a temp filename
            var tempPath = Path.Combine(Path.GetDirectoryName(path), Guid.NewGuid().ToString());
            var backup = path + ".backup";

            if (File.Exists(backup))
                File.Delete(backup);

            var data = Encoding.UTF8.GetBytes(contents);

            using (var tempFile = File.Create(tempPath, 4096, FileOptions.WriteThrough))
                tempFile.Write(data, 0, data.Length);

            // We need to make sure the file exists otherwise it'll throw an error if we try to use File.Replace
            if (File.Exists(path))
                File.Replace(tempPath, path, backup);
            else
                File.Move(tempPath, path);
        }

        public static async Task WriteAllTextAsync(string path, string contents)
        {
            // generate a temp filename
            var tempPath = Path.Combine(Path.GetDirectoryName(path), Guid.NewGuid().ToString());
            var backup = path + ".backup";

            if (File.Exists(backup))
                File.Delete(backup);

            var data = Encoding.UTF8.GetBytes(contents);

            using (var tempFile = File.Create(tempPath, 4096, FileOptions.WriteThrough))
                await tempFile.WriteAsync(data, 0, data.Length);

            // We need to make sure the file exists otherwise it'll throw an error if we try to use File.Replace
            if (File.Exists(path))
                File.Replace(tempPath, path, backup);
            else
                File.Move(tempPath, path);
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string SizeSuffix(long value, int decimalPlaces = 1, bool showSuffix = true)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "}{1}", 0, showSuffix ? " bytes" : ""); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                (showSuffix) ? SizeSuffixes[mag] : "");
        }

        /// <summary>
        /// 7zip LZMA Extraction
        /// Assumes it is single file and not an archive
        /// </summary>
        /// <param name="inFile">Source file</param>
        /// <param name="outFile">Target file</param>
        /// <param name="cleanup">Delete source file? True by default</param>
        /// <exception cref="Exception"></exception>
        public static void DecompressFileLZMA(string inFile, string outFile, bool cleanup = true)
        {
            if (File.Exists(outFile))
                File.Delete(outFile);

            using (FileStream input = new FileStream(inFile, FileMode.Open))
            using (FileStream output = new FileStream(outFile, FileMode.Create))
            {
                SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

                byte[] properties = new byte[5];
                if (input.Read(properties, 0, 5) != 5)
                    throw new Exception("input .lzma is too short");
                decoder.SetDecoderProperties(properties);

                byte[] sizeBytes = new byte[8];
                if (input.Read(sizeBytes, 0, 8) != 8)
                    throw new Exception("input .lzma is too short");

                long outSize = BitConverter.ToInt64(sizeBytes, 0);
                long compressedSize = input.Length - input.Position;
                decoder.Code(input, output, compressedSize, outSize, null);
            }

            if (cleanup)
                File.Delete(inFile);
        }

        /// <summary>
        /// 7zip LZMA Stream Extraction
        /// Same as DecompressFileLZMA but with split parts being merged into a single stream
        /// Still assuming it is a single file and not an archive
        /// </summary>
        /// <param name="directory">Directory of parted files</param>
        /// <param name="files">File list part files we're merging, requires it to be alphabetically sorted first</param>
        /// <param name="outFile">Target file</param>
        /// <param name="cleanup">Delete split parts? True by default</param>
        /// <returns>Empty string if no errors</returns>
        public static string DecompressStreamLZMA(string directory, List<string> files, string outFile, bool cleanup = true)
        {
            string status = string.Empty;
            string fullOutFile = Path.Combine(directory, outFile);
            if (File.Exists(fullOutFile))
                File.Delete(fullOutFile);

            try
            {
                //new FileStream(fullOutFile, FileMode.Create, FileAccess.Write)
                using (var output = new FileStream(fullOutFile, FileMode.Create))
                using (var input = new ConcatStream(files.Select(file => File.OpenRead(Path.Combine(directory, file)))))
                {
                    var decoder = new SevenZip.Compression.LZMA.Decoder();

                    byte[] properties = new byte[5];
                    if (input.Read(properties, 0, 5) != 5)
                        throw (new Exception("input .lzma is too short"));
                    decoder.SetDecoderProperties(properties);

                    byte[] sizeBytes = new byte[8];
                    if (input.Read(sizeBytes, 0, 8) != 8)
                        throw (new Exception("input .lzma is too short"));

                    long outSize = BitConverter.ToInt64(sizeBytes, 0);
                    long compressedSize = input.Length - 13;
                    decoder.Code(input, output, compressedSize, outSize, null);
                }

                // only delete files if successful
                if (cleanup)
                    files.ForEach(f => File.Delete(Path.Combine(directory, f)));
            }
            catch (Exception ex)
            {
                //Logger.log.Error("Functions::Extraction::DecompressStreamLZMA\nType: {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                status = string.Format("Failed to create {0}, Data Error due to missing parts", outFile);
            }

            return status;
        }

        public static bool DeltaPatch(string original, string patch)
        {
            string targetFileName = Path.GetFileName(original);

            try
            {
                using (FileStream originalFile = File.OpenRead(original))
                using (FileStream patchFile = File.OpenRead(patch))
                using (FileStream targetFile = File.Open(Path.Combine(Path.GetDirectoryName(patch), targetFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    VcdiffDecoder.Decode(originalFile, patchFile, targetFile);

                return true;
            }
            catch (Exception ex)
            {
                //errorLog.Add(string.Format("{0} Failed to delta - {1}", Path.GetFileName(patch), ex.Message));
                return false;
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Zip file extraction
        /// Supports sub-archives
        /// </summary>
        /// <param name="sourceZipFilePath">Source file</param>
        /// <param name="destinationDirectoryName">Extraction directory</param>
        /// <param name="overwrite">Overwrite files</param>
        /// <param name="sameDirectory">Write all files to one directory</param>
        /// <exception cref="IOException"></exception>
        public static void ExtractZipFileToDirectory(string sourceZipFilePath, string destinationDirectoryName, bool overwrite, bool sameDirectory = false)
        {
            using (var archive = ZipFile.Open(sourceZipFilePath, ZipArchiveMode.Read))
            {
                if (!overwrite)
                {
                    archive.ExtractToDirectory(destinationDirectoryName);
                    return;
                }

                DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
                string destinationDirectoryFullPath = di.FullName;

                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, sameDirectory ? file.Name : file.FullName));
                    if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Trying to extract file outside of destination directory.");

                    if (file.Name == "" && !sameDirectory)
                    {// Assuming Empty for Directory
                        Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                        continue;
                    }
                    file.ExtractToFile(completeFileName, true);
                }
            }
        }

        /// <summary>
        /// Zip file extraction
        /// </summary>
        /// <param name="stream">Source Stream</param>
        /// <param name="destinationDirectoryName">Extraction Directory</param>
        /// <param name="overwrite">Overwrite files</param>
        /// <param name="sameDirectory">Write all files to one directory</param>
        /// <exception cref="IOException"></exception>
        public static void ExtractZipFileToDirectory(Stream stream, string destinationDirectoryName, bool overwrite, bool sameDirectory = false)
        {
            using (ZipArchive archive = new ZipArchive(stream))
            {
                if (!overwrite)
                {
                    archive.ExtractToDirectory(destinationDirectoryName);
                    return;
                }

                DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
                string destinationDirectoryFullPath = di.FullName;

                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, sameDirectory ? file.Name : file.FullName));

                    if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Trying to extract file outside of destination directory.");

                    if (file.Name == "" && !sameDirectory)
                    {// Assuming Empty for Directory
                        Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                        continue;
                    }
                    file.ExtractToFile(completeFileName, true);
                }
            }
        }
    }
}
