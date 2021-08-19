using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using AzDocs.CheckCompanySpecificPR.Models;
using AzDocs.CheckCompanySpecificPR.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzDocs.CheckCompanySpecificPR.Tests
{
    public class CheckPrFunctionTest
    {
        private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
        private IConfiguration _config;

        public CheckPrFunctionTest()
        {
            _config = new ConfigurationBuilder().AddInMemoryCollection(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("CompanySpecificTerms:0", "test0"),
                new KeyValuePair<string, string>("AcceptedTerms:0", "test")
            }).Build();
        }

        /// <summary>
        /// Tests whether the function returns 200 OK during the happy path
        /// </summary>
        /// <returns>200 OK</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_Should_Return_200_OK()
        {
            // Arrange
            var data = "{\"resource\": {\"repository\": {\"id\": \"51520d10-3796-4ace-9235-9d1354829276\",\"project\": {\"id\": \"36c74ac1-de12-4114-aa8a-995c9bea22ac\"}},\"pullRequestId\": 6429,\"sourceRefName\": \"refs/heads/test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            _fixture.Inject(_config);

            // Not creating GitItem with AutoFixture because of circular references in the object (from third party library) and certain validation on the values. For now, this is the fastest solution.
            var gitItem = new GitItem
            {
                Content = _fixture.Create<string>(),
                Path = _fixture.Create<string>()
            };

            _fixture.Customize<GitPullRequestChange>(composer => composer.With(p => p.Item, gitItem));
            var pullRequestIterationChanges = _fixture.Create<GitPullRequestIterationChanges>();

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>())).ReturnsAsync((0, 1));
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>())).ReturnsAsync(pullRequestIterationChanges);
            githubClientServiceMock.Setup(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>())).ReturnsAsync(gitItem);

            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();
            compareServiceMock
                .Setup(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                .Returns(_fixture.CreateMany<CompanySpecificTermFound>(3).ToList());

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Once);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Exactly(3));
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Once);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Once);
        }

        /// <summary>
        /// Tests whether the function returns 200 OK during the happy path when no words are found
        /// </summary>
        /// <returns>200 OK</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_WithNoWordsFound_Should_Return_200_OK()
        {
            // Arrange
            var data = "{\"resource\": {\"repository\": {\"id\": \"51520d10-3796-4ace-9235-9d1354829276\",\"project\": {\"id\": \"36c74ac1-de12-4114-aa8a-995c9bea22ac\"}},\"pullRequestId\": 6429,\"sourceRefName\": \"refs/heads/test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            _fixture.Inject(_config);

            // Not creating GitItem with AutoFixture because of circular references in the object (from third party library) and certain validation on the values. For now, this is the fastest solution.
            var gitItem = new GitItem
            {
                Content = _fixture.Create<string>(),
                Path = _fixture.Create<string>()
            };

            _fixture.Customize<GitPullRequestChange>(composer => composer.With(p => p.Item, gitItem));
            var pullRequestIterationChanges = _fixture.Create<GitPullRequestIterationChanges>();

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>())).ReturnsAsync((0, 1));
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>())).ReturnsAsync(pullRequestIterationChanges);
            githubClientServiceMock.Setup(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>())).ReturnsAsync(gitItem);

            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();
            compareServiceMock
                .Setup(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                .Returns(_fixture.CreateMany<CompanySpecificTermFound>(0).ToList());

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Once);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Never);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Once);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Once);
        }

        /// <summary>
        /// Tests whether the function returns 200 OK during the happy path when the git file that is being checked has no content
        /// </summary>
        /// <returns>200 OK</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_WithGitFileNoContent_Should_Return_200_OK()
        {
            // Arrange
            var data = "{\"resource\": {\"repository\": {\"id\": \"51520d10-3796-4ace-9235-9d1354829276\",\"project\": {\"id\": \"36c74ac1-de12-4114-aa8a-995c9bea22ac\"}},\"pullRequestId\": 6429,\"sourceRefName\": \"refs/heads/test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            _fixture.Inject(_config);

            // Not creating GitItem with AutoFixture because of circular references in the object (from third party library) and certain validation on the values. For now, this is the fastest solution.
            var gitItem = new GitItem
            {
                Path = _fixture.Create<string>()
            };

            _fixture.Customize<GitPullRequestChange>(composer => composer.With(p => p.Item, gitItem));
            var pullRequestIterationChanges = _fixture.Create<GitPullRequestIterationChanges>();

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>())).ReturnsAsync((0, 1));
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>())).ReturnsAsync(pullRequestIterationChanges);
            githubClientServiceMock.Setup(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>())).ReturnsAsync(gitItem);

            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();
            compareServiceMock
                .Setup(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                .Returns(_fixture.CreateMany<CompanySpecificTermFound>(0).ToList());

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Once);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Never);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Once);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
        }

        /// <summary>
        /// Tests whether the function returns Internal Server Error when no Company Specific Terms are found
        /// </summary>
        /// <returns>500 Internal Server Error</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_Without_CompanySpecificTerms_Should_Return_500_InternalServerError()
        {
            // Arrange
            _config = new ConfigurationBuilder().AddInMemoryCollection(new List<KeyValuePair<string, string>>()).Build();
            _fixture.Inject(_config);

            var data = "{\"resource\": {\"repository\": {\"id\": \"51520d10-3796-4ace-9235-9d1354829276\",\"project\": {\"id\": \"36c74ac1-de12-4114-aa8a-995c9bea22ac\"}},\"pullRequestId\": 6429,\"sourceRefName\": \"refs/heads/test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Never);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Never);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Never);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Never);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Once);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
        }

        /// <summary>
        /// Tests whether the function returns Bad Request when the HttpRequestMessage has no content
        /// </summary>
        /// <returns>400 Bad Request</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_HttpRequestMessageHasNoContent_Should_Return_400_BadRequest()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage();

            _fixture.Inject(_config);

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Never);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Never);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Never);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Never);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Never);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
        }

        /// <summary>
        /// Tests whether the function returns Bad Request when the HttpRequestMessage has invalid content
        /// </summary>
        /// <returns>400 Bad Request</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_HttpRequestMessageHasInvalidContent_Should_Return_400_BadRequest()
        {
            // Arrange
            var data = "{\"test\": {\"test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };
            _fixture.Inject(_config);

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Never);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Never);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Never);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Never);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Never);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
        }

        /// <summary>
        /// Tests whether the function returns Internal Server Error when GetPullRequestIterations throws exception
        /// </summary>
        /// <returns>500 Internal Server Error</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_GetPullRequestIterationsThrowsException_Should_Return_500_InternalServerError()
        {
            // Arrange
            var data = "{\"resource\": {\"repository\": {\"id\": \"51520d10-3796-4ace-9235-9d1354829276\",\"project\": {\"id\": \"36c74ac1-de12-4114-aa8a-995c9bea22ac\"}},\"pullRequestId\": 6429,\"sourceRefName\": \"refs/heads/test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            _fixture.Inject(_config);

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            githubClientServiceMock
                .Setup(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()))
                .ThrowsAsync(new Exception());

            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Never);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Never);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Never);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Once);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
        }

        /// <summary>
        /// Tests whether the function returns Internal Server Error when GetPullRequestIterationChangesAsync throws exception
        /// </summary>
        /// <returns>500 Internal Server Error</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_GetPullRequestIterationChangesAsyncThrowsException_Should_Return_500_InternalServerError()
        {
            // Arrange
            var data = "{\"resource\": {\"repository\": {\"id\": \"51520d10-3796-4ace-9235-9d1354829276\",\"project\": {\"id\": \"36c74ac1-de12-4114-aa8a-995c9bea22ac\"}},\"pullRequestId\": 6429,\"sourceRefName\": \"refs/heads/test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            _fixture.Inject(_config);

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>())).ReturnsAsync((0, 1));
            githubClientServiceMock
                .Setup(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(),
                    It.IsAny<(int, int)>())).ThrowsAsync(new Exception());

            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Never);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Never);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Once);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
        }

        /// <summary>
        /// Tests whether the function returns Internal Server Error when GetPullRequestItemAsync throws exception
        /// </summary>
        /// <returns>500 Internal Server Error</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_GetPullRequestItemAsyncThrowsException_Should_Return_500_InternalServerError()
        {
            // Arrange
            var data = "{\"resource\": {\"repository\": {\"id\": \"51520d10-3796-4ace-9235-9d1354829276\",\"project\": {\"id\": \"36c74ac1-de12-4114-aa8a-995c9bea22ac\"}},\"pullRequestId\": 6429,\"sourceRefName\": \"refs/heads/test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            _fixture.Inject(_config);

            // Not creating GitItem with AutoFixture because of circular references in the object (from third party library) and certain validation on the values. For now, this is the fastest solution.
            var gitItem = new GitItem
            {
                Content = _fixture.Create<string>(),
                Path = _fixture.Create<string>()
            };

            _fixture.Customize<GitPullRequestChange>(composer => composer.With(p => p.Item, gitItem));
            var pullRequestIterationChanges = _fixture.Create<GitPullRequestIterationChanges>();

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>())).ReturnsAsync((0, 1));
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>())).ReturnsAsync(pullRequestIterationChanges);
            githubClientServiceMock.Setup(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>())).ThrowsAsync(new Exception());

            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Once);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Never);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Once);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
        }

        /// <summary>
        /// Tests whether the function returns Internal Server Error when CreateThreadOnPrAsync throws exception
        /// </summary>
        /// <returns>500 Internal Server Error</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_CreateThreadOnPrAsyncThrowsException_Should_Return_500_InternalServerError()
        {
            // Arrange
            var data = "{\"resource\": {\"repository\": {\"id\": \"51520d10-3796-4ace-9235-9d1354829276\",\"project\": {\"id\": \"36c74ac1-de12-4114-aa8a-995c9bea22ac\"}},\"pullRequestId\": 6429,\"sourceRefName\": \"refs/heads/test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            _fixture.Inject(_config);

            // Not creating GitItem with AutoFixture because of circular references in the object (from third party library) and certain validation on the values. For now, this is the fastest solution.
            var gitItem = new GitItem
            {
                Content = _fixture.Create<string>(),
                Path = _fixture.Create<string>()
            };

            _fixture.Customize<GitPullRequestChange>(composer => composer.With(p => p.Item, gitItem));
            var pullRequestIterationChanges = _fixture.Create<GitPullRequestIterationChanges>();

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>())).ReturnsAsync((0, 1));
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>())).ReturnsAsync(pullRequestIterationChanges);
            githubClientServiceMock.Setup(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>())).ReturnsAsync(gitItem);
            githubClientServiceMock.Setup(x => x.CreateThreadOnPrAsync(It.IsAny<string>(),
                It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(),
                It.IsAny<CompanySpecificTermFound>())).Throws<Exception>();

            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();
            compareServiceMock
                .Setup(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                .Returns(_fixture.CreateMany<CompanySpecificTermFound>(3).ToList());
            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Once);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Once);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Once);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Once);
        }

        /// <summary>
        /// Tests whether the function returns Internal Server Error when FindCompanySpecificTerm throws exception
        /// </summary>
        /// <returns>500 Internal Server Error</returns>
        [Fact]
        public async Task ExecuteCheckPrFunction_FindCompanySpecificTermThrowsException_Should_Return_500_InternalServerError()
        {
            // Arrange
            var data = "{\"resource\": {\"repository\": {\"id\": \"51520d10-3796-4ace-9235-9d1354829276\",\"project\": {\"id\": \"36c74ac1-de12-4114-aa8a-995c9bea22ac\"}},\"pullRequestId\": 6429,\"sourceRefName\": \"refs/heads/test\"}}";
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json")
            };

            _fixture.Inject(_config);

            // Not creating GitItem with AutoFixture because of circular references in the object (from third party library) and certain validation on the values. For now, this is the fastest solution.
            var gitItem = new GitItem
            {
                Content = _fixture.Create<string>(),
                Path = _fixture.Create<string>()
            };

            _fixture.Customize<GitPullRequestChange>(composer => composer.With(p => p.Item, gitItem));
            var pullRequestIterationChanges = _fixture.Create<GitPullRequestIterationChanges>();

            var loggerMock = _fixture.Freeze<Mock<ILogger>>();
            var githubClientServiceMock = _fixture.Freeze<Mock<IGitHubClientService>>();
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>())).ReturnsAsync((0, 1));
            githubClientServiceMock.Setup(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>())).ReturnsAsync(pullRequestIterationChanges);
            githubClientServiceMock.Setup(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>())).ReturnsAsync(gitItem);

            var compareServiceMock = _fixture.Freeze<Mock<ICompareService>>();
            compareServiceMock
                .Setup(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<string>>()))
                .Throws<Exception>();

            var function = _fixture.Freeze<CheckCompanySpecificPr>();

            // Act
            var result = await function.Run(httpRequestMessage, loggerMock.Object);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationsToCompareAsync(It.IsAny<PullRequestInformation>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestIterationChangesAsync(It.IsAny<PullRequestInformation>(), It.IsAny<(int, int)>()), Times.Once);
            githubClientServiceMock.Verify(x => x.GetPullRequestItemAsync(It.IsAny<PullRequestInformation>(), It.IsAny<string>()), Times.Once);
            githubClientServiceMock.Verify(x => x.CreateThreadOnPrAsync(It.IsAny<string>(), It.IsAny<CommentThreadStatus>(), It.IsAny<CommentType>(), It.IsAny<PullRequestInformation>(), It.IsAny<CompanySpecificTermFound>()), Times.Never);
            githubClientServiceMock.Verify(x => x.SetStatusOfPrAsync(It.IsAny<PullRequestInformation>(), It.IsAny<GitStatusState>(), It.IsAny<string>()), Times.Once);
            compareServiceMock.Verify(x => x.FindCompanySpecificTerm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Once);
        }
    }
}
