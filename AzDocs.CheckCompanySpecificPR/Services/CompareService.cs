using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AzDocs.CheckCompanySpecificPR.Models;

namespace AzDocs.CheckCompanySpecificPR.Services
{
    public class CompareService : ICompareService
    {

        public List<CompanySpecificTermFound> FindCompanySpecificTerm(string text, string wordToFind, string filePath, List<string> acceptedTerms)
        {
            var termsFound = new List<CompanySpecificTermFound>();
            int lineNum = 0;
            using (StringReader reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()?.ToLower()) != null)
                {
                    lineNum++;
                    var matches = Regex.Matches(line, $"(?<word>[a-zA-Z]*{wordToFind.ToLower()}[a-zA-Z]*)");

                    if (matches.Any(x => x.Success))
                    {
                        // if term is accepted, continue
                        foreach (Match match in matches)
                        {
                            if (acceptedTerms.Select(x => x.ToLower()).Any(x => x == match.Value))
                            {
                                continue;
                            }

                            var start = match.Groups["word"].Index + 1;
                            var end = start + match.Groups["word"].Length;

                            termsFound.Add(new CompanySpecificTermFound { LineNumber = lineNum, Start = start, End = end, WordToFind = wordToFind, FilePath = filePath, WordFound = match.Value });
                        }
                    }
                }
            }

            return termsFound;
        }
    }
}
