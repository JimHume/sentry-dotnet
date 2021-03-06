using System;
using System.Collections.Generic;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.Tests.Extensibility
{
    public class RequestBodyExtractionDispatcherTests
    {
        private class Fixture
        {
            public SentryOptions SentryOptions { get; set; } = new SentryOptions();
            public RequestSize RequestSize { get; set; } = RequestSize.Small;
            public IHttpRequest HttpRequest { get; set; } = Substitute.For<IHttpRequest>();
            public IRequestPayloadExtractor Extractor { get; set; } = Substitute.For<IRequestPayloadExtractor>();
            public IEnumerable<IRequestPayloadExtractor> Extractors { get; set; }

            public Fixture()
            {
                HttpRequest.ContentLength.Returns(10);
                Extractor.ExtractPayload(Arg.Any<IHttpRequest>()).Returns("Result");
                Extractors = new[] { Extractor };
            }

            public RequestBodyExtractionDispatcher GetSut() => new RequestBodyExtractionDispatcher(Extractors, SentryOptions, () => RequestSize);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void ExtractPayload_DefaultFixture_ReadsMockPayload()
        {
            var sut = _fixture.GetSut();

            var actual = sut.ExtractPayload(_fixture.HttpRequest);
            Assert.Same("Result", actual);
        }

        [Fact]
        public void ExtractPayload_ExtractorEmptyString_ReturnsNull()
        {
            _fixture.Extractor.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(string.Empty);
            var sut = _fixture.GetSut();

            Assert.Null(sut.ExtractPayload(_fixture.HttpRequest));
        }

        [Fact]
        public void ExtractPayload_ExtractorNull_ReturnsNull()
        {
            _fixture.Extractor.ExtractPayload(Arg.Any<IHttpRequest>()).ReturnsNull();
            var sut = _fixture.GetSut();

            Assert.Null(sut.ExtractPayload(_fixture.HttpRequest));
        }

        [Theory]
        [InlineData(RequestSize.None, 1, false)]
        [InlineData(RequestSize.Small, 999, true)]
        [InlineData(RequestSize.Small, 10_000, false)]
        [InlineData(RequestSize.Medium, 9999, true)]
        [InlineData(RequestSize.Medium, 100_000, false)]
        [InlineData(RequestSize.Always, int.MaxValue, true)] // 2 GB event? No problem...
        public void ExtractPayload_RequestSizeSmall_ContentLength(RequestSize requestSize, int contentLength, bool readBody)
        {
            _fixture.RequestSize = requestSize;
            _fixture.HttpRequest.ContentLength.Returns(contentLength);
            var sut = _fixture.GetSut();

            Assert.Equal(readBody, sut.ExtractPayload(_fixture.HttpRequest) != null);
        }

        [Fact]
        public void ExtractPayload_NullRequest_ReturnsNull()
        {
            var sut = _fixture.GetSut();
            Assert.Null(sut.ExtractPayload(null));
        }

        [Fact]
        public void Ctor_NullExtractors_ThrowsArgumentNullException()
        {
            _fixture.Extractors = null;
            Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
        }

        [Fact]
        public void Ctor_NullOptions_ThrowsArgumentNullException()
        {
            _fixture.SentryOptions = null;
            Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
        }
    }
}
