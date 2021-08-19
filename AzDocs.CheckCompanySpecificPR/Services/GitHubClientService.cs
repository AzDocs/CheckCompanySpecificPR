using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzDocs.CheckCompanySpecificPR.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzDocs.CheckCompanySpecificPR.Services
{
    public class GitHubClientService : IGitHubClientService
    {
        private readonly IGitHttpClientWrapper _client;
        public GitHubClientService(IGitHttpClientWrapper client)
        {
            _client = client;
        }

        public async Task<(int, int)> GetPullRequestIterationsToCompareAsync(PullRequestInformation pr)
        {
            // get iterations
            var iterations = await _client.GetPullRequestIterationsAsync(pr.Resource.Repository.Project.Id, pr.Resource.Repository.Id, pr.Resource.PullRequestId);
            if (iterations.Count == 0)
            {
                throw new Exception("No iterations found for this pull request. Something is wrong. Terminating.");
            }

            // get the last two iterations, if there is only 1 iteration, compare it with the base (0)
            var currentIteration = iterations.Count == 1 ? 1 : iterations[^2].Id.GetValueOrDefault();
            var iterationToCompare = iterations.Count == 1 ? 0 : iterations[^1].Id.GetValueOrDefault();

            return (currentIteration, iterationToCompare);
        }

        public async Task SetStatusOfPrAsync(PullRequestInformation pr, GitStatusState state, string description)
        {
            var status = new GitPullRequestStatus
            {
                Context = new GitStatusContext
                {
                    Name = "companyspecific-status-check",
                    Genre = "pr-azure-function-ci"
                },
                State = state,
                Description = description
            };

            //create status
            await _client.CreatePullRequestStatusAsync(status, pr.Resource.Repository.Id, pr.Resource.PullRequestId);
        }

        public async Task<GitItem> GetPullRequestItemAsync(PullRequestInformation pr, string path)
        {
            GitVersionDescriptor gitDes = new GitVersionDescriptor { Version = pr.Resource.SourceRefName.Replace("refs/heads/", "") };
            return await _client.GetItemAsync(pr.Resource.Repository.Id, path, true,  gitDes);
        }

        public async Task<GitPullRequestIterationChanges> GetPullRequestIterationChangesAsync(PullRequestInformation pr, (int currentIteration , int iterationToCompare) iterationsToCompare)
        {
            var compare = await _client.GetPullRequestIterationChangesAsync(pr.Resource.Repository.Project.Id, pr.Resource.Repository.Id, pr.Resource.PullRequestId, iterationsToCompare.currentIteration, iterationsToCompare.iterationToCompare);
            return compare;
        }

        public async Task CreateThreadOnPrAsync(string message, CommentThreadStatus status, CommentType commentType, PullRequestInformation pr, CompanySpecificTermFound specificTermFound)
        {
            if (await ThreadExistOnPr(pr, message, specificTermFound))
            {
                return;
            }

            var thread = CreateThread(message, status, commentType, specificTermFound);
            await _client.CreateThreadAsync(thread, pr.Resource.Repository.Project.Id, pr.Resource.Repository.Id, pr.Resource.PullRequestId);
        }

        private GitPullRequestCommentThread CreateThread(string message, CommentThreadStatus status, CommentType commentType, CompanySpecificTermFound specificTermFound)
        {
            var listComment = new List<Comment>
            {
                new Comment
                {
                    Content = message,
                    CommentType = commentType
                }
            };

            var startPosition = new CommentPosition
            {
                Line = specificTermFound.LineNumber,
                Offset = specificTermFound.Start
            };

            var endPosition = new CommentPosition
            {
                Line = specificTermFound.LineNumber,
                Offset = specificTermFound.End
            };

            var gitPullRequestThread = new GitPullRequestCommentThread
            {
                Status = status,
                Comments = listComment,
                ThreadContext = new CommentThreadContext
                {
                    FilePath = specificTermFound.FilePath,
                    RightFileStart = startPosition,
                    RightFileEnd = endPosition
                },
            };

            return gitPullRequestThread;
        }

        private async Task<bool> ThreadExistOnPr(PullRequestInformation pr, string message, CompanySpecificTermFound specificTermFound)
        {
            var threads = await _client.GetThreadsAsync(pr.Resource.Repository.Project.Id, pr.Resource.Repository.Id, pr.Resource.PullRequestId);
            foreach (var thread in threads)
            {
                if (thread.ThreadContext.RightFileEnd.Line == specificTermFound.LineNumber && thread.ThreadContext.RightFileEnd.Offset == specificTermFound.End &&
                    thread.ThreadContext.RightFileStart.Line == specificTermFound.LineNumber && thread.ThreadContext.RightFileStart.Offset == specificTermFound.Start &&
                    thread.Comments.Any(x => x.Content.Equals(message)) &&
                    thread.ThreadContext.FilePath == specificTermFound.FilePath
                    )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
