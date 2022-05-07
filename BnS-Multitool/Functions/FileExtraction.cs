using BnS_Multitool.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BnS_Multitool.Functions
{
    /// <summary>
    /// Commonly used functions for file extraction
    /// </summary>
    static class FileExtraction
    {
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
                if(!overwrite)
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

            if(cleanup)
                File.Delete(inFile);
        }

        public static bool ExtractArchiveLZMA(string inFile, string destinationDirectory)
        {
            try
            {
                using(var extracter = new SevenZip.SevenZipExtractor(File.OpenRead(inFile)))
                {
                    foreach(var fileName in extracter.ArchiveFileData)
                    {
                        using(var fs = new FileStream(Path.Combine(destinationDirectory, fileName.FileName), FileMode.Create))
                        {
                            extracter.ExtractFile(fileName.Index, fs);
                            int bufferSize = 4069;
                            byte[] buffer = new byte[bufferSize];
                            int bytesRead = 0;
                            while ((bytesRead = fs.Read(buffer, 0, bufferSize)) != 0)
                                fs.Write(buffer, 0, bytesRead);
                        }
                    }
                    return true;
                }
            } catch (Exception ex)
            {
                Logger.log.Error(ex.ToString());
                return false;
            }
        }

        public static bool IsFileLocked(string file)
        {
            try
            {
                using (var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None))
                    stream.Close();
            } catch (IOException)
            {
                return true;
            }
            return false;
        }

        private static void CompressFileLZMA(string inFile, string outFile)
        {
            SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();
            FileStream input = new FileStream(inFile, FileMode.Open);
            FileStream output = new FileStream(outFile, FileMode.Create);

            // Write the encoder properties
            coder.WriteCoderProperties(output);

            // Write the decompressed file size.
            output.Write(BitConverter.GetBytes(input.Length), 0, 8);

            // Encode the file.
            coder.Code(input, output, input.Length, -1, null);
            output.Flush();
            output.Close();
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
                Logger.log.Error("Functions::Extraction::DecompressStreamLZMA\nType: {0}\n{1}\n{2}", ex.GetType().Name, ex.ToString(), ex.StackTrace);
                status = string.Format("Failed to create {0}, Data Error due to missing parts", outFile);
            }

            return status;
        }
    }
}
