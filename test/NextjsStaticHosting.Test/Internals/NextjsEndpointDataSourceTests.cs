using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace NextjsStaticHosting.Internals.Test
{
    public class NextjsEndpointDataSourceTests
    {
        [Fact]
        public void Basics_Work()
        {
            // Arrange
            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(@"Y:\test\中文");
            var options = Options.Create(new NextjsStaticHostingOptions { RootPath = "a/ê" });

            var fileProvider = new Mock<IFileProvider>();
            fileProvider
                .Setup(f => f.GetDirectoryContents(string.Empty))
                .Returns(new TestDirectoryContents(new TestFile("abc.html"), new TestFile("d&f.html"), new TestFile("[id].html"), new TestDirectory("nested 中文")));
            fileProvider
                .Setup(f => f.GetDirectoryContents("/nested 中文"))
                .Returns(new TestDirectoryContents(new TestFile("123.html"), new TestFile("[...slug].html"), new TestFile("234.jpg")));

            var fileProviderFactory = new Mock<FileProviderFactory>();
            fileProviderFactory.Setup(f => f.CreateFileProvider(@"Y:\test\中文\a/ê")).Returns(fileProvider.Object);
            var staticFileOptionsProvider = new StaticFileOptionsProvider(env.Object, fileProviderFactory.Object, options);

            var appBuilder = new Mock<IApplicationBuilder>();
            appBuilder.Setup(a => a.Build()).Returns(_ => Task.CompletedTask);
            var endpointRouteBuilder = new Mock<IEndpointRouteBuilder>();
            endpointRouteBuilder
                .Setup(e => e.CreateApplicationBuilder())
                .Returns(appBuilder.Object);

            var sut = new NextjsEndpointDataSource(endpointRouteBuilder.Object, staticFileOptionsProvider);

            // Act
            var endpoints = sut.Endpoints;

            // Assert
            endpoints.Count.Should().Be(5);
            endpoints[0].DisplayName.Should().Be("Next.js /abc.html");
            endpoints[0].Metadata.GetMetadata<NextjsEndpointDataSource.StaticFileEndpointMetadata>().Path.Should().Be("/abc.html");
            GetPatternString(endpoints[0]).Should().Be("abc");

            endpoints[1].DisplayName.Should().Be("Next.js /d&f.html");
            endpoints[1].Metadata.GetMetadata<NextjsEndpointDataSource.StaticFileEndpointMetadata>().Path.Should().Be("/d&f.html");
            GetPatternString(endpoints[1]).Should().Be("d&f");

            endpoints[2].DisplayName.Should().Be("Next.js /[id].html");
            endpoints[2].Metadata.GetMetadata<NextjsEndpointDataSource.StaticFileEndpointMetadata>().Path.Should().Be("/[id].html");
            GetPatternString(endpoints[2]).Should().Be("{id}");

            endpoints[3].DisplayName.Should().Be("Next.js /nested 中文/123.html");
            endpoints[3].Metadata.GetMetadata<NextjsEndpointDataSource.StaticFileEndpointMetadata>().Path.Should().Be("/nested 中文/123.html");
            GetPatternString(endpoints[3]).Should().Be("nested 中文/123");

            endpoints[4].DisplayName.Should().Be("Next.js /nested 中文/[...slug].html");
            endpoints[4].Metadata.GetMetadata<NextjsEndpointDataSource.StaticFileEndpointMetadata>().Path.Should().Be("/nested 中文/[...slug].html");
            GetPatternString(endpoints[4]).Should().Be("nested 中文/{*slug}");

            static string GetPatternString(Endpoint endpoint)
            {
                var routeEndpoint = (RouteEndpoint)endpoint;
                var method = typeof(RoutePattern).GetMethod("DebuggerToString", BindingFlags.NonPublic | BindingFlags.Instance);
                return (string)method.Invoke(routeEndpoint.RoutePattern, null);
            }
        }

        private class TestDirectoryContents : IDirectoryContents, IFileInfo
        {
            private readonly IEnumerable<IFileInfo> files;

            public TestDirectoryContents(params IFileInfo[] files)
            {
                this.files = files;
            }

            public bool Exists => true;

            public long Length => throw new NotSupportedException();

            public string PhysicalPath => throw new NotSupportedException();

            public string Name => throw new NotSupportedException();

            public DateTimeOffset LastModified => throw new NotSupportedException();

            public bool IsDirectory => true;

            public Stream CreateReadStream()
            {
                throw new NotSupportedException();
            }

            public IEnumerator<IFileInfo> GetEnumerator() => this.files.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }

        private class TestFileInfo : IFileInfo
        {
            protected TestFileInfo(string name, bool isDirectory)
            {
                this.Name = name;
                this.IsDirectory = isDirectory;
            }

            public string Name { get; }

            public bool IsDirectory { get; }

            public bool Exists => throw new NotImplementedException();

            public DateTimeOffset LastModified => throw new NotImplementedException();

            public long Length => throw new NotImplementedException();

            public string PhysicalPath => throw new NotImplementedException();

            public Stream CreateReadStream() => throw new NotImplementedException();
        }

        private class TestDirectory : TestFileInfo
        {
            public TestDirectory(string name)
                : base(name, isDirectory: true)
            { }
        }

        private class TestFile : TestFileInfo
        {
            public TestFile(string name)
                : base(name, isDirectory: false)
            { }
        }
    }
}
