using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzDocs.CheckCompanySpecificPR.Services
{
    public class GitHttpClientWrapper : IGitHttpClientWrapper
    {
        private readonly GitHttpClient _client;
        public GitHttpClientWrapper(GitHttpClient client)
        {
            _client = client;
        }

        public async Task<List<GitPullRequestIteration>> GetPullRequestIterationsAsync(Guid projectId,
            string repositoryId, int pullRequestId)
        {
            return await _client.GetPullRequestIterationsAsync(projectId, repositoryId, pullRequestId);
        }

        public async Task<GitPullRequestStatus> CreatePullRequestStatusAsync(GitPullRequestStatus state, string repositoryId, int pullRequestId)
        {
            return await _client.CreatePullRequestStatusAsync(state, repositoryId, pullRequestId);
        }

        public async Task<GitItem> GetItemAsync(string repositoryId, string path, bool includeContent, GitVersionDescriptor gitDes)
        {
            return await _client.GetItemAsync(repositoryId, path, includeContent: includeContent, versionDescriptor: gitDes);
        }

        public async Task<GitPullRequestIterationChanges> GetPullRequestIterationChangesAsync(Guid projectId, string repositoryId, int pullRequestId, int iterationId,
            int compareTo)
        {
            return await _client.GetPullRequestIterationChangesAsync(projectId, repositoryId, pullRequestId, iterationId, compareTo);
        }

        public async Task<List<GitPullRequestCommentThread>> GetThreadsAsync(Guid projectId, string repositoryId, int pullRequestId)
        {
            return await _client.GetThreadsAsync(projectId, repositoryId, pullRequestId);
        }

        public async Task<GitPullRequestCommentThread> CreateThreadAsync(GitPullRequestCommentThread commentThread, Guid projectId, string repositoryId,
            int pullRequestId)
        {
            return await _client.CreateThreadAsync(commentThread, projectId, repositoryId, pullRequestId);
        }
    }
}
