using BnsDatTool;
using BnsDatTool.lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Originally made by Sedro
 * https://github.com/Sedro01/BnsPerformanceFix/
*/

namespace BnsPerformanceFix
{
    public class BnsPerformanceFix
    {
        public static string FilterLocalDatInPlace(string datfile, Filter filter)
        {
            var is64 = datfile.Contains("64");
            var extracted = ExtractDat(datfile, is64);
            var binfile = Path.Combine(extracted, is64 ? "localfile64.bin" : "localfile.bin");
            string outprint = FilterLocalBinInPlace(binfile, filter);
            CompressDat(extracted, is64);
            return outprint;
        }

        public static string FilterLocalBinInPlace(string binfile, Filter filter)
        {
            var is64 = binfile.EndsWith("64.bin");

            var bdat = new BDat();
            using (var stream = new FileStream(binfile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream))
            {
                bdat.Load(reader, BXML_TYPE.BXML_BINARY, is64);
            }

            var text = bdat.Content.Lists
                .Select(list => list.Collection.Archive)
                .SingleOrDefault(archive => archive != null);

            if (text == null)
            {
                throw new Exception($"Could not find text table in {binfile}");
            }

            var filtered = text.SubArchives
                .Select(sub => ApplyFilter(sub, filter))
                .Where(x => x != null)
                .ToArray();

            var countBefore = text.SubArchives.Sum(sub => sub.FieldLookupCount);
            var countAfter = filtered.Sum(sub => sub.FieldLookupCount);
            //Console.WriteLine($"Kept {countAfter} out of {countBefore} text strings ({countAfter / (float)countBefore:P})");
            string outprint = $"Kept {countAfter} out of {countBefore} text strings ({countAfter / (float)countBefore:P})";

            text.SubArchives = filtered;
            text.SubArchiveCount = filtered.Length;

            using (var stream = new FileStream(binfile, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                bdat.Save(writer, BXML_TYPE.BXML_BINARY, is64);
            }

            return outprint;
        }

        static BDAT_SUBARCHIVE ApplyFilter(BDAT_SUBARCHIVE sub, Filter filter)
        {
            var filtered = sub.Fields
                .Zip(sub.Lookups, ValueTuple.Create)
                .Where(zipped =>
                {
                    var (field, lookup) = zipped;
                    var words = new BDat().LookupSplitToWords(lookup.Data, lookup.Size);
                    var alias = words[0];
                    var text = words[1];

                    return filter.Matches(alias);
                })
                .ToArray();

            if (filtered.Length == 0)
            {
                return null;
            }
            else if (filtered.Length == sub.FieldLookupCount)
            {
                return sub;
            }
            else
            {
                var fields = filtered.Select(zipped => zipped.Item1).ToArray();
                var lookups = filtered.Select(zipped => zipped.Item2).ToArray();

                var startEnd = new byte[16];
                using (var stream = new MemoryStream(startEnd))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write((long)fields.First().ID);
                    writer.Write((long)fields.Last().ID);
                }

                return new BDAT_SUBARCHIVE
                {
                    Fields = fields,
                    Lookups = lookups,
                    FieldLookupCount = fields.Length,
                    StartAndEndFieldId = startEnd
                };
            }
        }

        public static string ExtractDat(string path, bool is64)
        {
            new BNSDat().Extract(path, ReportProgress, is64);
            Console.WriteLine();
            return $"{path}.files";
        }

        static string CompressDat(string path, bool is64)
        {
            new BNSDat().Compress(path, ReportProgress, is64);
            return path.Replace(".files", "");
        }

        static void ReportProgress(int step, int total) { }
    }
}
