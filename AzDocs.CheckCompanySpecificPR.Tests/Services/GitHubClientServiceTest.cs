using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using AzDocs.CheckCompanySpecificPR.Models;
using AzDocs.CheckCompanySpecificPR.Services;
using FluentAssertions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Moq;
using Xunit;

namespace AzDocs.CheckCompanySpecificPR.Tests.Services
{
    public class GitHubClientServiceTest
    {
        private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());

        /// <summary>
        /// Tests whether GetPullRequestIterationsToCompareAsync, when there's only 1 iteration, returns the values 1 (the first iteration) and 0 (the base) to compare to. 
        /// </summary>
        /// <returns>Iterations to compare</returns>
        [Fact]
        public async Task GetPullRequestIterationsToCompareAsync_WithOneIteration_Should_Return_Value()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();

            // Act

            var gitPullRequestIteration = new List<GitPullRequestIteration>
            {
                new GitPullRequestIteration()
            };
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x => x.GetPullRequestIterationsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(gitPullRequestIteration);

            var githubClientService = _fixture.Freeze<GitHubClientService>();
            var result = await githubClientService.GetPullRequestIterationsToCompareAsync(pr);

            // Assert
            result.Item1.Should().Be(1);
            result.Item2.Should().Be(0);

            client.Verify(x => x.GetPullRequestIterationsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// Tests whether GetPullRequestIterationsToCompareAsync, when there are multiple iterations, returns the values for the last two iterations to compare.
        /// </summary>
        /// <returns>Iterations to compare</returns>
        [Fact]
        public async Task GetPullRequestIterationsToCompareAsync_WithMultipleIterations_Should_Return_Value()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();
            var gitPullRequestIteration = new List<GitPullRequestIteration>
            {
                new GitPullRequestIteration
                {
                    Id = 20
                },
                new GitPullRequestIteration
                {
                    Id = 21
                },
                new GitPullRequestIteration
                {
                    Id = 22
                }
            };

            // Act
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x => x.GetPullRequestIterationsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(gitPullRequestIteration);

            var githubClientService = _fixture.Freeze<GitHubClientService>();
            var result = await githubClientService.GetPullRequestIterationsToCompareAsync(pr);

            // Assert
            result.Item1.Should().Be(21);
            result.Item2.Should().Be(22);

            client.Verify(x => x.GetPullRequestIterationsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// Tests whether GetPullRequestIterationsToCompareAsync, when there are no iterations throws an exception. 
        /// </summary>
        /// <returns>Exception</returns>
        [Fact]
        public async Task GetPullRequestIterationsToCompareAsync_WithNoIterations_Should_Throw_Exception()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();
            var gitPullRequestIteration = new List<GitPullRequestIteration>();

            // Act
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x => x.GetPullRequestIterationsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(gitPullRequestIteration);

            var githubClientService = _fixture.Freeze<GitHubClientService>();

            // Assert
            await Assert.ThrowsAsync<Exception>(() => githubClientService.GetPullRequestIterationsToCompareAsync(pr));
            client.Verify(x => x.GetPullRequestIterationsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// Tests whether GetPullRequestItemAsync returns an item
        /// </summary>
        /// <returns>GitItem</returns>
        [Fact]
        public async Task GetPullRequestItemAsync_Should_Return_Item()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();

            // Act
            var gitItem = new GitItem();
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x => x.GetItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<GitVersionDescriptor>())).ReturnsAsync(gitItem);

            var githubClientService = _fixture.Freeze<GitHubClientService>();
            var result = await githubClientService.GetPullRequestItemAsync(pr, _fixture.Create<string>());

            // Assert
            result.Should().NotBeNull();

            client.Verify(x => x.GetItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<GitVersionDescriptor>()), Times.Once);
        }

        /// <summary>
        /// Tests whether GetPullRequestItemAsync throws an exception
        /// </summary>
        /// <returns>Exception</returns>
        [Fact]
        public async Task GetPullRequestItemAsync_Throws_Exception()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();

            // Act
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x => x.GetItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<GitVersionDescriptor>()))
                .ThrowsAsync(new Exception());

            var githubClientService = _fixture.Freeze<GitHubClientService>();

            // Assert
            await Assert.ThrowsAsync<Exception>(() => githubClientService.GetPullRequestItemAsync(pr, _fixture.Create<string>()));
            client.Verify(x => x.GetItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<GitVersionDescriptor>()), Times.Once);
        }

        /// <summary>
        /// Tests whether SetStatusOfPrAsync succeeds
        /// </summary>
        [Fact]
        public async Task SetStatusOfPrAsync_Succeeds()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();

            // Act
            var gitItem = new GitItem();
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x =>
                x.CreatePullRequestStatusAsync(It.IsAny<GitPullRequestStatus>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(new GitPullRequestStatus());

            var githubClientService = _fixture.Freeze<GitHubClientService>();
            await githubClientService.SetStatusOfPrAsync(pr, GitStatusState.Succeeded, _fixture.Create<string>());

            // Assert

            client.Verify(x => x.CreatePullRequestStatusAsync(It.IsAny<GitPullRequestStatus>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// Tests whether SetStatusOfPrAsync throws exception
        /// </summary>
        [Fact]
        public async Task SetStatusOfPrAsync_ThrowsException()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();

            // Act
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x =>
                x.CreatePullRequestStatusAsync(It.IsAny<GitPullRequestStatus>(), It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new Exception());

            var githubClientService = _fixture.Freeze<GitHubClientService>();

            // Assert
            await Assert.ThrowsAsync<Exception>(() => githubClientService.SetStatusOfPrAsync(pr, GitStatusState.Succeeded, _fixture.Create<string>()));
            client.Verify(x => x.CreatePullRequestStatusAsync(It.IsAny<GitPullRequestStatus>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// Tests whether GetPullRequestIterationChanges returns changes
        /// </summary>
        /// <returns>GitPullRequestIterationChanges</returns>
        [Fact]
        public async Task GetPullRequestIterationChanges_Returns_Changes()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();

            // Act
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x =>
                x.GetPullRequestIterationChangesAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new GitPullRequestIterationChanges());

            var githubClientService = _fixture.Freeze<GitHubClientService>();
            await githubClientService.GetPullRequestIterationChangesAsync(pr, (1, 0));

            // Assert
            client.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// Tests whether GetPullRequestIterationChanges throws exception
        /// </summary>
        /// <returns>Exception</returns>
        [Fact]
        public async Task GetPullRequestIterationChanges_ThrowsException()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();

            // Act
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x =>
                x.GetPullRequestIterationChangesAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception());

            var githubClientService = _fixture.Freeze<GitHubClientService>();

            // Assert
            await Assert.ThrowsAsync<Exception>(() => githubClientService.GetPullRequestIterationChangesAsync(pr, (1, 0)));
            client.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// Tests whether CreateThreadOnPrAsync with an existing thread, does not create a new duplicate thread
        /// </summary>
        [Fact]
        public async Task CreateThreadOnPrAsync_WithExistingThread_Should_Not_CreateNewThread()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();
            var message = "This is a test";
            var path = _fixture.Create<string>();
            int line = 1;
            int start = 1;
            int end = 5;

            var specificTermFound = new CompanySpecificTermFound()
            {
                FilePath = path,
                End = end,
                LineNumber = line,
                Start = start
            };

            var pullRequestComments = new List<GitPullRequestCommentThread>
            {
                new GitPullRequestCommentThread
                {
                    ThreadContext = new CommentThreadContext
                    {
                        FilePath = path,
                        RightFileEnd = new CommentPosition
                        {
                            Line = line,
                            Offset = end
                        },
                        RightFileStart = new CommentPosition
                        {
                            Line = line,
                            Offset = start
                        }
                    },
                    Comments = new List<Comment>
                    {
                        new Comment
                        {
                            Content = message
                        }
                    }
                }
            };

            // Act
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x => x.CreateThreadAsync(It.IsAny<GitPullRequestCommentThread>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()));
            client.Setup(x => x.GetThreadsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(pullRequestComments);

            var githubClientService = _fixture.Freeze<GitHubClientService>();
            await githubClientService.CreateThreadOnPrAsync(message, It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), pr, specificTermFound);

            // Assert
            client.Verify(x => x.GetThreadsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            client.Verify(x => x.CreateThreadAsync(It.IsAny<GitPullRequestCommentThread>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests whether CreateThreadOnPrAsync with no existing thread, creates a new thread
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateThreadOnPrAsync_WithNoExistingThread_Should_CreateNewThread()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();
            var message = "This is a test";
            var path = _fixture.Create<string>();
            int line = 1;

            var specificTermFound = new CompanySpecificTermFound()
            {
                FilePath = path,
                End = 2,
                LineNumber = line,
                Start = 3
            };

            var pullRequestComments = new List<GitPullRequestCommentThread>
            {
                new GitPullRequestCommentThread
                {
                    ThreadContext = new CommentThreadContext
                    {
                        FilePath = path,
                        RightFileEnd = new CommentPosition
                        {
                            Line = line,
                            Offset = 5
                        },
                        RightFileStart = new CommentPosition
                        {
                            Line = line,
                            Offset = 1
                        }
                    },
                    Comments = new List<Comment>
                    {
                        new Comment
                        {
                            Content = "This is another test"
                        }
                    }
                }
            };

            // Act
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x => x.CreateThreadAsync(It.IsAny<GitPullRequestCommentThread>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(new GitPullRequestCommentThread());
            client.Setup(x => x.GetThreadsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(pullRequestComments);

            var githubClientService = _fixture.Freeze<GitHubClientService>();
            await githubClientService.CreateThreadOnPrAsync(message, It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), pr, specificTermFound);

            // Assert
            client.Verify(x => x.GetThreadsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            client.Verify(x => x.CreateThreadAsync(It.IsAny<GitPullRequestCommentThread>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// Tests whether CreateThreadOnPrAsync throws exception
        /// </summary>
        /// <returns>Exception</returns>
        [Fact]
        public async Task CreateThreadOnPrAsync_ThrowsException()
        {
            // Arrange
            var pr = _fixture.Create<PullRequestInformation>();
            var message = "This is a test";
            var path = _fixture.Create<string>();
            int line = 1;

            var specificTermFound = new CompanySpecificTermFound()
            {
                FilePath = path,
                End = 2,
                LineNumber = line,
                Start = 3
            };

            var pullRequestComments = new List<GitPullRequestCommentThread>
            {
                new GitPullRequestCommentThread
                {
                    ThreadContext = new CommentThreadContext
                    {
                        FilePath = path,
                        RightFileEnd = new CommentPosition
                        {
                            Line = line,
                            Offset = 5
                        },
                        RightFileStart = new CommentPosition
                        {
                            Line = line,
                            Offset = 1
                        }
                    },
                    Comments = new List<Comment>
                    {
                        new Comment
                        {
                            Content = "This is another test"
                        }
                    }
                }
            };

            // Act
            var client = _fixture.Freeze<Mock<IGitHttpClientWrapper>>();
            client.Setup(x => x.CreateThreadAsync(It.IsAny<GitPullRequestCommentThread>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new Exception());
            client.Setup(x => x.GetThreadsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(pullRequestComments);

            var githubClientService = _fixture.Freeze<GitHubClientService>();

            // Assert
            await Assert.ThrowsAsync<Exception>(() => githubClientService.CreateThreadOnPrAsync(message, It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), pr, specificTermFound));
            client.Verify(x => x.GetThreadsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            client.Verify(x => x.CreateThreadAsync(It.IsAny<GitPullRequestCommentThread>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

    }
}
