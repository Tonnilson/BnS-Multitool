using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BnsPerformanceFix
{
    public class Filter
    {
        public Filter(IReadOnlyList<Rule> rules)
        {
            Rules = rules;
        }

        public IReadOnlyList<Rule> Rules { get; }

        public bool Matches(string alias)
        {
            foreach (var rule in Rules)
            {
                if (rule.Pattern.IsMatch(alias))
                {
                    return rule.Include;
                }
            }

            return false;
        }

        public static Filter Load(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return new Filter(Parse(reader).ToList());
            }
        }

        private static IEnumerable<Rule> Parse(StreamReader reader)
        {
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    // EOF
                    break;
                }

                var parsed = Rule.Parse(line);
                if (parsed != null)
                {
                    yield return parsed;
                }
            }
        }

        public class Rule
        {
            public bool Include { get; set; }
            public Regex Pattern { get; set; }

            public static Rule Parse(string line)
            {
                Regex BuildRegex(string filter)
                {
                    var pattern = Regex.Escape(filter)
                        .Replace(@"\*", @".*") // * wildcard
                        .Replace(@"\?", @"."); // ? wildcard

                    return new Regex($"^{pattern}$", RegexOptions.IgnoreCase);
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    // whitespace
                    return null;
                }
                else if (line.StartsWith("#"))
                {
                    // comment
                    return null;
                }
                else if (line.StartsWith("-"))
                {
                    // exclude
                    var pattern = line.Substring(1).TrimStart(' ');
                    return new Rule { Include = false, Pattern = BuildRegex(pattern) };
                }
                else if (Regex.IsMatch(line, @"^[\w*?]"))
                {
                    // include
                    return new Rule { Include = true, Pattern = BuildRegex(line) };
                }
                else
                {
                    throw new Exception($"Invalid rule syntax: {line}");
                }
            }
        }
    }
}
