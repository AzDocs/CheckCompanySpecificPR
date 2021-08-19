using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using AzDocs.CheckCompanySpecificPR.Services;
using FluentAssertions;
using Xunit;

namespace AzDocs.CheckCompanySpecificPR.Tests.Services
{
    public class CompareServiceTest
    {
        private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());

        /// <summary>
        /// Tests whether the amount of words 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="wordToFind"></param>
        /// <param name="path"></param>
        /// <param name="acceptedTerms"></param>
        /// <param name="expectedResultCount"></param>
        [Theory]
        [ClassData(typeof(TestData))]
        public void FindCompanySpecificTerm_Returns_ExpectedResultCount(string text, string wordToFind, List<string> acceptedTerms, int expectedResultCount, int expectedLineNumber, int expectedStart, int expectedEnd)
        {
            // Arrange 
            var path = "path";

            // Act
            var compareService = _fixture.Freeze<CompareService>();
            var result = compareService.FindCompanySpecificTerm(text, wordToFind, path, acceptedTerms);

            // Assert
            result.Count.Should().Be(expectedResultCount);
            result.First().LineNumber.Should().Be(expectedLineNumber);
            result.First().Start.Should().Be(expectedStart);
            result.First().End.Should().Be(expectedEnd);
        }


        /// <summary>
        /// Tests whether, when no results are found, no results are returned
        /// </summary>
        [Fact]
        public void FindCompanySpecificTerm_Returns_NoResults()
        {
            var text = "This is a line\r\nThis is a helloTest line";
            var wordToFind = "Hellotest";
            var acceptedTerms = new List<string> {"Hellotest"};
            var path = "path";

            // Act
            var compareService = _fixture.Freeze<CompareService>();
            var result = compareService.FindCompanySpecificTerm(text, wordToFind, path, acceptedTerms);

            // Assert
            result.Count.Should().Be(0);
        }
    }
}

public class TestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "This is a test", "test", new List<string> { "test1234" }, 1, 1, 11, 15 };
        yield return new object[] { "This is a line\r\nThis is a helloTest line", "Hellotest", new List<string> { "test" }, 1, 2, 11, 20 };
        yield return new object[] { "This is a test line\r\nThis is a helloTest line", "Hellotest", new List<string> { "test" }, 1, 2, 11, 20 };
        yield return new object[] { "This is a test line\r\nThis is a helloTest line", "test", new List<string> { "test" }, 1, 2, 11, 20 };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
