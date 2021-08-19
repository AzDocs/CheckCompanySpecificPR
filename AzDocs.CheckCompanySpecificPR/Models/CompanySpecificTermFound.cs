namespace AzDocs.CheckCompanySpecificPR.Models
{
    public class CompanySpecificTermFound
    {
        public int LineNumber { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public string WordToFind { get; set; }
        public string WordFound { get; set; }
        public string FilePath { get; set; }
    }
}
