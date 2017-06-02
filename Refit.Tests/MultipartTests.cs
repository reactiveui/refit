using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Refit;

namespace Refit.Tests
{
    public interface IRunscopeApi
    {
        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadStream(Stream stream);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadStreamPart(StreamPart stream);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadBytes(byte[] bytes);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadBytesPart([AliasAs("ByteArrayPartParamAlias")]ByteArrayPart bytes);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadString([AliasAs("SomeStringAlias")]string someString);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadFileInfo(IEnumerable<FileInfo> fileInfos, FileInfo anotherFile);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadFileInfoPart(IEnumerable<FileInfoPart> fileInfos, FileInfoPart anotherFile);
    }

    public class MultipartTests
    {
        // To test: sign up for a Runscope account (it's free, despite them implying that's its only good for 30 days)
        // and then insert your bucket URI here in order to run tests and verify success via the Runscope UI
        const string runscopeUri = "https://.runscope.net/";

        [Fact(Skip = "Set runscopeUri field to your Runscope key in order to test this function.")]
        public async Task MultipartUploadShouldWorkWithStream()
        {
            using (var stream = GetTestFileStream("Test Files/Test.pdf")) {
                var fixture = RestService.For<IRunscopeApi>(runscopeUri);
                var result = await fixture.UploadStream(stream);

                Assert.True(result.IsSuccessStatusCode);
            }
        }

        [Fact(Skip = "Set runscopeUri field to your Runscope key in order to test this function.")]
        public async Task MultipartUploadShouldWorkWithByteArray()
        {
            using (var stream = GetTestFileStream("Test Files/Test.pdf"))
            using (var reader = new BinaryReader(stream)) {
                var bytes = reader.ReadBytes((int)stream.Length);

                var fixture = RestService.For<IRunscopeApi>(runscopeUri);
                var result = await fixture.UploadBytes(bytes);

                Assert.True(result.IsSuccessStatusCode);
            }
        }

        [Fact(Skip = "Set runscopeUri field to your Runscope key in order to test this function.")]
        public async Task MultipartUploadShouldWorkWithFileInfo()
        {
            var fileName = Path.GetTempFileName();

            try {
                using (var stream = GetTestFileStream("Test Files/Test.pdf"))
                using (var outStream = File.OpenWrite(fileName)) {
                    await stream.CopyToAsync(outStream);
                    await outStream.FlushAsync();
                    outStream.Close();

                    var fixture = RestService.For<IRunscopeApi>(runscopeUri);
                    var result = await fixture.UploadFileInfo(new [] { new FileInfo(fileName), new FileInfo(fileName) }, new FileInfo(fileName));

                    Assert.True(result.IsSuccessStatusCode);
                }
            } finally {
                File.Delete(fileName);
            }
        }

        [Fact(Skip = "Set runscopeUri field to your Runscope key in order to test this function.")]
        public async Task MultipartUploadShouldWorkWithString()
        {
            const string text = "This is random text";

            var fixture = RestService.For<IRunscopeApi>(runscopeUri);
            var result = await fixture.UploadString(text);

            Assert.True(result.IsSuccessStatusCode);
        }

        [Fact(Skip = "Set runscopeUri field to your Runscope key in order to test this function.")]
        public async Task MultipartUploadShouldWorkWithStreamPart()
        {
            using (var stream = GetTestFileStream("Test Files/Test.pdf"))
            {
                var fixture = RestService.For<IRunscopeApi>(runscopeUri);
                var result = await fixture.UploadStreamPart(new StreamPart(stream, "test-streampart.pdf", "application/pdf"));

                Assert.True(result.IsSuccessStatusCode);
            }
        }

        [Fact(Skip = "Set runscopeUri field to your Runscope key in order to test this function.")]
        public async Task MultipartUploadShouldWorkWithByteArrayPart()
        {
            using (var stream = GetTestFileStream("Test Files/Test.pdf"))
            using (var reader = new BinaryReader(stream))
            {
                var bytes = reader.ReadBytes((int)stream.Length);

                var fixture = RestService.For<IRunscopeApi>(runscopeUri);
                var result = await fixture.UploadBytesPart(new ByteArrayPart(bytes, "test-bytearraypart.pdf", "application/pdf"));

                Assert.True(result.IsSuccessStatusCode);
            }
        }

        [Fact(Skip = "Set runscopeUri field to your Runscope key in order to test this function.")]
        public async Task MultipartUploadShouldWorkWithFileInfoPart()
        {
            var fileName = Path.GetTempFileName();

            try
            {
                using (var stream = GetTestFileStream("Test Files/Test.pdf"))
                using (var outStream = File.OpenWrite(fileName))
                {
                    await stream.CopyToAsync(outStream);
                    await outStream.FlushAsync();
                    outStream.Close();

                    var fixture = RestService.For<IRunscopeApi>(runscopeUri);
                    var result = await fixture.UploadFileInfoPart(new[] 
                    {
                        new FileInfoPart(new FileInfo(fileName), "test-fileinfopart.pdf", "application/pdf"),
                        new FileInfoPart(new FileInfo(fileName), "test-fileinfopart2.pdf", contentType: null)
                    }, new FileInfoPart(new FileInfo(fileName), fileName: "additionalfile.pdf", contentType: "application/pdf"));

                    Assert.True(result.IsSuccessStatusCode);
                }
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        [Fact]
        public void MultiPartConstructorShouldThrowArgumentNullExceptionWhenNoFileName()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                var byteArrayPart = new ByteArrayPart(new byte[0], null, "application/pdf");
            });
        }

        [Fact]
        public void FileInfoPartConstructorShouldThrowArgumentNullExceptionWhenNoFileInfo()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                var fileInfoPart = new FileInfoPart(null, "file.pdf", "application/pdf");
            });
        }

        private static Stream GetTestFileStream(string relativeFilePath)
        {
            const char namespaceSeparator = '.';

            // get calling assembly
            var assembly = Assembly.GetCallingAssembly();

            // compute resource name suffix
            var relativeName = "." + relativeFilePath
                .Replace('\\', namespaceSeparator)
                .Replace('/', namespaceSeparator)
                .Replace(' ', '_');

            // get resource stream
            var fullName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(relativeName, StringComparison.InvariantCulture));
            if (fullName == null) {
                throw new Exception(string.Format("Unable to find resource for path \"{0}\". Resource with name ending on \"{1}\" was not found in assembly.", relativeFilePath, relativeName));
            }

            var stream = assembly.GetManifestResourceStream(fullName);
            if (stream == null) {
                throw new Exception(string.Format("Unable to find resource for path \"{0}\". Resource named \"{1}\" was not found in assembly.", relativeFilePath, fullName));
            }

            return stream;
        }
    }
}
