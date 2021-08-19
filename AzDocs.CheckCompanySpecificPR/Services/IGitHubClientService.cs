using System.Collections.Generic;
using System.Threading.Tasks;
using AzDocs.CheckCompanySpecificPR.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzDocs.CheckCompanySpecificPR.Services
{
    public interface IGitHubClientService
    {
        Task<(int, int)> GetPullRequestIterationsToCompareAsync(PullRequestInformation pr);
        Task SetStatusOfPrAsync(PullRequestInformation pr, GitStatusState state, string description);

        Task<GitPullRequestIterationChanges> GetPullRequestIterationChangesAsync(PullRequestInformation pr,
            (int currentIteration, int iterationToCompare) iterationsToCompare);

        Task CreateThreadOnPrAsync(string message, CommentThreadStatus status, CommentType commentType,
            PullRequestInformation pr, CompanySpecificTermFound specificTermFound);

        Task<GitItem> GetPullRequestItemAsync(PullRequestInformation pr, string path);

    }
}
