using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzDocs.CheckCompanySpecificPR.Services
{
    public interface IGitHttpClientWrapper
    {
        Task<List<GitPullRequestIteration>> GetPullRequestIterationsAsync(Guid projectId, string repositoryId, int pullRequestId);

        Task<GitPullRequestStatus> CreatePullRequestStatusAsync(GitPullRequestStatus state, string repositoryId, int pullRequestId);

        Task<GitItem> GetItemAsync(string repositoryId, string path, bool includeContent, GitVersionDescriptor gitDes);
        Task<GitPullRequestIterationChanges> GetPullRequestIterationChangesAsync(Guid projectId, string repositoryId, int pullRequestId, int iterationId, int compareTo);

        Task<List<GitPullRequestCommentThread>> GetThreadsAsync(Guid projectId, string repositoryId, int pullRequestId);

        Task<GitPullRequestCommentThread> CreateThreadAsync(GitPullRequestCommentThread commentThread, Guid projectId, string repositoryId, int pullRequestId);
    }
}
