using System.Collections.Generic;
using AzDocs.CheckCompanySpecificPR.Models;

namespace AzDocs.CheckCompanySpecificPR.Services
{
    public interface ICompareService
    {
        List<CompanySpecificTermFound> FindCompanySpecificTerm(string text, string wordToFind, string filePath,
            List<string> acceptedTerms);
    }
}
