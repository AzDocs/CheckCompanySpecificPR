using System;

namespace AzDocs.CheckCompanySpecificPR.Models
{
    public class PullRequestInformation
    {
        public Resource Resource { get; set; }
    }

    public class Resource
    {
        public Repository Repository { get; set; }
        public int PullRequestId { get; set; }
        public string SourceRefName { get; set; }

    }

    public class Repository
    {
        public string Id { get; set; }
        public Project Project { get; set; }
    }

    public class Project
    {
        public Guid Id { get; set; }
    }

}
