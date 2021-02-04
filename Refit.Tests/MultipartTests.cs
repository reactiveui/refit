using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Refit;
using System.Threading;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;

namespace Refit.Tests
{
    public interface IRunscopeApi
    {
        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadStream(Stream stream);

        [Multipart("-----SomeCustomBoundary")]
        [Post("/")]
        Task<HttpResponseMessage> UploadStreamWithCustomBoundary(Stream stream);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadStreamPart(StreamPart stream);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadStreamPart([Query] ModelObject someQueryParams, StreamPart stream);

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
        Task<HttpResponseMessage> UploadStringWithHeaderAndRequestProperty([Header("Authorization")] string authorization, [Property("SomeProperty")] string someProperty, [AliasAs("SomeStringAlias")]string someString);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadFileInfo(IEnumerable<FileInfo> fileInfos, FileInfo anotherFile);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadFileInfoPart(IEnumerable<FileInfoPart> fileInfos, FileInfoPart anotherFile);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadJsonObject(ModelObject theObject);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadJsonObjects(IEnumerable<ModelObject> theObjects);


        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadMixedObjects(IEnumerable<ModelObject> theObjects, AnotherModel anotherModel, FileInfo aFile, AnEnum anEnum, string aString, int anInt);

        [Multipart]
        [Post("/")]
        Task<HttpResponseMessage> UploadHttpContent(HttpContent content);

    }

    public class ModelObject
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
    }

    public class AnotherModel
    {
        public string[] Foos { get; set; }
    }

    public enum AnEnum
    {
        Val1,
        Val2
    }

    public class MultipartTests
    {
        const string BaseAddress = "https://api/";

        [Fact]
        public async Task MultipartUploadShouldWorkWithStream()
        {
            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("stream", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal("stream", parts[0].Headers.ContentDisposition.FileName);

                    using var str = await parts[0].ReadAsStreamAsync();
                    using var src = GetTestFileStream("Test Files/Test.pdf");
                    Assert.True(StreamsEqual(src, str));
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            using var stream = GetTestFileStream("Test Files/Test.pdf");
            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadStream(stream);
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithStreamAndCustomBoundary()
        {
            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("stream", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal("stream", parts[0].Headers.ContentDisposition.FileName);

                    using var str = await parts[0].ReadAsStreamAsync();
                    using var src = GetTestFileStream("Test Files/Test.pdf");
                    Assert.True(StreamsEqual(src, str));
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            using (var stream = GetTestFileStream("Test Files/Test.pdf"))
            {
                var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
                var result = await fixture.UploadStreamWithCustomBoundary(stream);
            }

            var input = typeof(IRunscopeApi);
            var methodFixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "UploadStreamWithCustomBoundary"));
            Assert.Equal("-----SomeCustomBoundary", methodFixture.MultipartBoundary);
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithByteArray()
        {
            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("bytes", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal("bytes", parts[0].Headers.ContentDisposition.FileName);
                    Assert.Null(parts[0].Headers.ContentType);
                    using var str = await parts[0].ReadAsStreamAsync();
                    using var src = GetTestFileStream("Test Files/Test.pdf");
                    Assert.True(StreamsEqual(src, str));
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            using var stream = GetTestFileStream("Test Files/Test.pdf");
            using var reader = new BinaryReader(stream);
            var bytes = reader.ReadBytes((int)stream.Length);

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadBytes(bytes);
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithFileInfo()
        {
            var fileName = Path.GetTempFileName();
            var name = Path.GetFileName(fileName);

            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Equal(3, parts.Count);

                    Assert.Equal("fileInfos", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal(name, parts[0].Headers.ContentDisposition.FileName);
                    Assert.Null(parts[0].Headers.ContentType);
                    using (var str = await parts[0].ReadAsStreamAsync())
                    using (var src = GetTestFileStream("Test Files/Test.pdf"))
                    {
                        Assert.True(StreamsEqual(src, str));
                    }

                    Assert.Equal("fileInfos", parts[1].Headers.ContentDisposition.Name);
                    Assert.Equal(name, parts[1].Headers.ContentDisposition.FileName);
                    Assert.Null(parts[1].Headers.ContentType);
                    using (var str = await parts[1].ReadAsStreamAsync())
                    using (var src = GetTestFileStream("Test Files/Test.pdf"))
                    {
                        Assert.True(StreamsEqual(src, str));
                    }

                    Assert.Equal("anotherFile", parts[2].Headers.ContentDisposition.Name);
                    Assert.Equal(name, parts[2].Headers.ContentDisposition.FileName);
                    Assert.Null(parts[2].Headers.ContentType);
                    using (var str = await parts[2].ReadAsStreamAsync())
                    using (var src = GetTestFileStream("Test Files/Test.pdf"))
                    {
                        Assert.True(StreamsEqual(src, str));
                    }

                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            try
            {
                using var stream = GetTestFileStream("Test Files/Test.pdf");
                using var outStream = File.OpenWrite(fileName);
                await stream.CopyToAsync(outStream);
                await outStream.FlushAsync();
                outStream.Close();

                var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
                var result = await fixture.UploadFileInfo(new[] { new FileInfo(fileName), new FileInfo(fileName) }, new FileInfo(fileName));
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithString()
        {
            const string text = "This is random text";

            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("SomeStringAlias", parts[0].Headers.ContentDisposition.Name);
                    Assert.Null(parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal("text/plain", parts[0].Headers.ContentType.MediaType);
                    Assert.Equal("utf-8", parts[0].Headers.ContentType.CharSet);
                    var str = await parts[0].ReadAsStringAsync();
                    Assert.Equal(text, str);
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };


            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadString(text);
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithHeaderAndRequestProperty()
        {
            const string text = "This is random text";
            const string someHeader = "someHeader";
            const string someProperty = "someProperty";

            var handler = new MockHttpMessageHandler
            {
                RequestAsserts = message =>
                {
                    Assert.Equal(someHeader, message.Headers.Authorization.ToString());

#if NET5_0_OR_GREATER
                    Assert.Equal(2, message.Options.Count());
                    Assert.Equal(someProperty, ((IDictionary<string, object>)message.Options)["SomeProperty"]);
#endif

#pragma warning disable CS0618 // Type or member is obsolete
                    Assert.Equal(2, message.Properties.Count);
                    Assert.Equal(someProperty, message.Properties["SomeProperty"]);
#pragma warning restore CS0618 // Type or member is obsolete
                },
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("SomeStringAlias", parts[0].Headers.ContentDisposition.Name);
                    Assert.Null(parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal("text/plain", parts[0].Headers.ContentType.MediaType);
                    Assert.Equal("utf-8", parts[0].Headers.ContentType.CharSet);
                    var str = await parts[0].ReadAsStringAsync();
                    Assert.Equal(text, str);
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };


            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadStringWithHeaderAndRequestProperty(someHeader,someProperty, text);
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithStreamPart()
        {
            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("stream", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal("test-streampart.pdf", parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/pdf", parts[0].Headers.ContentType.MediaType);

                    using var str = await parts[0].ReadAsStreamAsync();
                    using var src = GetTestFileStream("Test Files/Test.pdf");
                    Assert.True(StreamsEqual(src, str));
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            using var stream = GetTestFileStream("Test Files/Test.pdf");
            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadStreamPart(new StreamPart(stream, "test-streampart.pdf", "application/pdf"));
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithStreamPartWithNamedMultipart()
        {
            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("test-stream", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal("test-streampart.pdf", parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/pdf", parts[0].Headers.ContentType.MediaType);

                    using var str = await parts[0].ReadAsStreamAsync();
                    using var src = GetTestFileStream("Test Files/Test.pdf");
                    Assert.True(StreamsEqual(src, str));
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            using var stream = GetTestFileStream("Test Files/Test.pdf");
            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadStreamPart(new StreamPart(stream, "test-streampart.pdf", "application/pdf", "test-stream"));
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithStreamPartAndQuery()
        {
            var handler = new MockHttpMessageHandler
            {
                RequestAsserts = request =>
                {
                    Assert.Equal("?Property1=test&Property2=test2", request.RequestUri.Query);
                },
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("stream", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal("test-streampart.pdf", parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/pdf", parts[0].Headers.ContentType.MediaType);

                    using var str = await parts[0].ReadAsStreamAsync();
                    using var src = GetTestFileStream("Test Files/Test.pdf");
                    Assert.True(StreamsEqual(src, str));
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            using var stream = GetTestFileStream("Test Files/Test.pdf");
            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadStreamPart(new ModelObject()
            {
                Property1 = "test",
                Property2 = "test2"
            }, new StreamPart(stream, "test-streampart.pdf", "application/pdf"));
        }


        [Fact]
        public async Task MultipartUploadShouldWorkWithByteArrayPart()
        {
            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("ByteArrayPartParamAlias", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal("test-bytearraypart.pdf", parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/pdf", parts[0].Headers.ContentType.MediaType);

                    using var str = await parts[0].ReadAsStreamAsync();
                    using var src = GetTestFileStream("Test Files/Test.pdf");
                    Assert.True(StreamsEqual(src, str));
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            using var stream = GetTestFileStream("Test Files/Test.pdf");
            using var reader = new BinaryReader(stream);
            var bytes = reader.ReadBytes((int)stream.Length);

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadBytesPart(new ByteArrayPart(bytes, "test-bytearraypart.pdf", "application/pdf"));
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithFileInfoPart()
        {
            var fileName = Path.GetTempFileName();

            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Equal(3, parts.Count);

                    Assert.Equal("fileInfos", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal("test-fileinfopart.pdf", parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/pdf", parts[0].Headers.ContentType.MediaType);
                    using (var str = await parts[0].ReadAsStreamAsync())
                    using (var src = GetTestFileStream("Test Files/Test.pdf"))
                    {
                        Assert.True(StreamsEqual(src, str));
                    }

                    Assert.Equal("fileInfos", parts[1].Headers.ContentDisposition.Name);
                    Assert.Equal("test-fileinfopart2.pdf", parts[1].Headers.ContentDisposition.FileName);
                    Assert.Null(parts[1].Headers.ContentType);
                    using (var str = await parts[1].ReadAsStreamAsync())
                    using (var src = GetTestFileStream("Test Files/Test.pdf"))
                    {
                        Assert.True(StreamsEqual(src, str));
                    }

                    Assert.Equal("anotherFile", parts[2].Headers.ContentDisposition.Name);
                    Assert.Equal("additionalfile.pdf", parts[2].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/pdf", parts[2].Headers.ContentType.MediaType);
                    using (var str = await parts[2].ReadAsStreamAsync())
                    using (var src = GetTestFileStream("Test Files/Test.pdf"))
                    {
                        Assert.True(StreamsEqual(src, str));
                    }

                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };


            try
            {
                using var stream = GetTestFileStream("Test Files/Test.pdf");
                using var outStream = File.OpenWrite(fileName);
                await stream.CopyToAsync(outStream);
                await outStream.FlushAsync();
                outStream.Close();

                var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
                var result = await fixture.UploadFileInfoPart(new[]
                {
                        new FileInfoPart(new FileInfo(fileName), "test-fileinfopart.pdf", "application/pdf"),
                        new FileInfoPart(new FileInfo(fileName), "test-fileinfopart2.pdf", contentType: null)
                    }, new FileInfoPart(new FileInfo(fileName), fileName: "additionalfile.pdf", contentType: "application/pdf"));
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        [Theory]
        [InlineData(typeof(NewtonsoftJsonContentSerializer), "application/json")]
        [InlineData(typeof(SystemTextJsonContentSerializer), "application/json")]
        [InlineData(typeof(XmlContentSerializer), "application/xml")]
        public async Task MultipartUploadShouldWorkWithAnObject(Type contentSerializerType, string mediaType)
        {
            if (Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
            {
                throw new ArgumentException($"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
            }

            var model1 = new ModelObject
            {
                Property1 = "M1.prop1",
                Property2 = "M1.prop2"
            };

            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("theObject", parts[0].Headers.ContentDisposition.Name);
                    Assert.Null(parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal(mediaType, parts[0].Headers.ContentType.MediaType);
                    var result0 = await serializer.FromHttpContentAsync<ModelObject>(parts[0]).ConfigureAwait(false);
                    Assert.Equal(model1.Property1, result0.Property1);
                    Assert.Equal(model1.Property2, result0.Property2);
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler,
                ContentSerializer = serializer
            };

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadJsonObject(model1);
        }

        [Theory]
        [InlineData(typeof(NewtonsoftJsonContentSerializer), "application/json")]
        [InlineData(typeof(SystemTextJsonContentSerializer), "application/json")]
        [InlineData(typeof(XmlContentSerializer), "application/xml")]
        public async Task MultipartUploadShouldWorkWithObjects(Type contentSerializerType, string mediaType)
        {
            if (Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
            {
                throw new ArgumentException($"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
            }

            var model1 = new ModelObject
            {
                Property1 = "M1.prop1",
                Property2 = "M1.prop2"
            };

            var model2 = new ModelObject
            {
                Property1 = "M2.prop1"
            };

            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Equal(2, parts.Count);

                    Assert.Equal("theObjects", parts[0].Headers.ContentDisposition.Name);
                    Assert.Null(parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal(mediaType, parts[0].Headers.ContentType.MediaType);
                    var result0 = await serializer.FromHttpContentAsync<ModelObject>(parts[0]).ConfigureAwait(false);
                    Assert.Equal(model1.Property1, result0.Property1);
                    Assert.Equal(model1.Property2, result0.Property2);


                    Assert.Equal("theObjects", parts[1].Headers.ContentDisposition.Name);
                    Assert.Null(parts[1].Headers.ContentDisposition.FileName);
                    Assert.Equal(mediaType, parts[1].Headers.ContentType.MediaType);
                    var result1 = await serializer.FromHttpContentAsync<ModelObject>(parts[1]).ConfigureAwait(false);
                    Assert.Equal(model2.Property1, result1.Property1);
                    Assert.Equal(model2.Property2, result1.Property2);
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler,
                ContentSerializer = serializer
            };

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadJsonObjects(new[] { model1, model2 });
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithMixedTypes()
        {
            var fileName = Path.GetTempFileName();
            var name = Path.GetFileName(fileName);

            var model1 = new ModelObject
            {
                Property1 = "M1.prop1",
                Property2 = "M1.prop2"
            };

            var model2 = new ModelObject
            {
                Property1 = "M2.prop1"
            };

            var anotherModel = new AnotherModel
            {
                Foos = new[] { "bar1", "bar2" }
            };

            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Equal(7, parts.Count);

                    Assert.Equal("theObjects", parts[0].Headers.ContentDisposition.Name);
                    Assert.Null(parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/json", parts[0].Headers.ContentType.MediaType);
                    var result0 = JsonConvert.DeserializeObject<ModelObject>(await parts[0].ReadAsStringAsync().ConfigureAwait(false));
                    Assert.Equal(model1.Property1, result0.Property1);
                    Assert.Equal(model1.Property2, result0.Property2);


                    Assert.Equal("theObjects", parts[1].Headers.ContentDisposition.Name);
                    Assert.Null(parts[1].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/json", parts[1].Headers.ContentType.MediaType);
                    var result1 = JsonConvert.DeserializeObject<ModelObject>(await parts[1].ReadAsStringAsync().ConfigureAwait(false));
                    Assert.Equal(model2.Property1, result1.Property1);
                    Assert.Equal(model2.Property2, result1.Property2);

                    Assert.Equal("anotherModel", parts[2].Headers.ContentDisposition.Name);
                    Assert.Null(parts[2].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/json", parts[2].Headers.ContentType.MediaType);
                    var result2 = JsonConvert.DeserializeObject<AnotherModel>(await parts[2].ReadAsStringAsync().ConfigureAwait(false));
                    Assert.Equal(2, result2.Foos.Length);
                    Assert.Equal("bar1", result2.Foos[0]);
                    Assert.Equal("bar2", result2.Foos[1]);


                    Assert.Equal("aFile", parts[3].Headers.ContentDisposition.Name);
                    Assert.Equal(name, parts[3].Headers.ContentDisposition.FileName);
                    Assert.Null(parts[3].Headers.ContentType);
                    using (var str = await parts[3].ReadAsStreamAsync())
                    using (var src = GetTestFileStream("Test Files/Test.pdf"))
                    {
                        Assert.True(StreamsEqual(src, str));
                    }

                    Assert.Equal("anEnum", parts[4].Headers.ContentDisposition.Name);
                    Assert.Null(parts[4].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/json", parts[4].Headers.ContentType.MediaType);
                    var result4 = JsonConvert.DeserializeObject<AnEnum>(await parts[4].ReadAsStringAsync().ConfigureAwait(false));
                    Assert.Equal(AnEnum.Val2, result4);

                    Assert.Equal("aString", parts[5].Headers.ContentDisposition.Name);
                    Assert.Null(parts[5].Headers.ContentDisposition.FileName);
                    Assert.Equal("text/plain", parts[5].Headers.ContentType.MediaType);
                    Assert.Equal("utf-8", parts[5].Headers.ContentType.CharSet);
                    Assert.Equal("frob", await parts[5].ReadAsStringAsync());

                    Assert.Equal("anInt", parts[6].Headers.ContentDisposition.Name);
                    Assert.Null(parts[6].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/json", parts[6].Headers.ContentType.MediaType);
                    var result6 = JsonConvert.DeserializeObject<int>(await parts[6].ReadAsStringAsync().ConfigureAwait(false));
                    Assert.Equal(42, result6);

                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            try
            {
                using var stream = GetTestFileStream("Test Files/Test.pdf");
                using var outStream = File.OpenWrite(fileName);
                await stream.CopyToAsync(outStream);
                await outStream.FlushAsync();
                outStream.Close();

                var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
                var result = await fixture.UploadMixedObjects(new[] { model1, model2 }, anotherModel, new FileInfo(fileName), AnEnum.Val2, "frob", 42);
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        [Fact]
        public async Task MultipartUploadShouldWorkWithHttpContent()
        {
            var httpContent = new StringContent("some text", Encoding.ASCII, "application/custom");
            httpContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                Name = "myName",
                FileName = "myFileName",
            };

            var handler = new MockHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var parts = content.ToList();

                    Assert.Single(parts);

                    Assert.Equal("myName", parts[0].Headers.ContentDisposition.Name);
                    Assert.Equal("myFileName", parts[0].Headers.ContentDisposition.FileName);
                    Assert.Equal("application/custom", parts[0].Headers.ContentType.MediaType);
                    var result0 = await parts[0].ReadAsStringAsync().ConfigureAwait(false);
                    Assert.Equal("some text", result0);
                }
            };

            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            var result = await fixture.UploadHttpContent(httpContent);
        }

        [Fact]
        public void MultiPartConstructorShouldThrowArgumentNullExceptionWhenNoFileName()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var byteArrayPart = new ByteArrayPart(Array.Empty<byte>(), null, "application/pdf");
            });
        }

        [Fact]
        public void FileInfoPartConstructorShouldThrowArgumentNullExceptionWhenNoFileInfo()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var fileInfoPart = new FileInfoPart(null, "file.pdf", "application/pdf");
            });
        }

        internal static Stream GetTestFileStream(string relativeFilePath)
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
            if (fullName == null)
            {
                throw new Exception($"Unable to find resource for path \"{relativeFilePath}\". Resource with name ending on \"{relativeName}\" was not found in assembly.");
            }

            var stream = assembly.GetManifestResourceStream(fullName);
            if (stream == null)
            {
                throw new Exception($"Unable to find resource for path \"{relativeFilePath}\". Resource named \"{fullName}\" was not found in assembly.");
            }

            return stream;
        }

        static bool StreamsEqual(Stream a, Stream b)
        {
            if (a == null &&
                b == null)
                return true;
            if (a == null ||
                b == null)
            {
                throw new ArgumentNullException(
                    a == null ? "a" : "b");
            }

            if (a.Length < b.Length)
                return false;
            if (a.Length > b.Length)
                return false;

            for (var i = 0; i < a.Length; i++)
            {
                var aByte = a.ReadByte();
                var bByte = b.ReadByte();
                if (aByte != bByte)
                    return false;
            }

            return true;
        }

        class MockHttpMessageHandler : HttpMessageHandler
        {
            public Action<HttpRequestMessage> RequestAsserts { get; set; }
            public Func<MultipartFormDataContent, Task> Asserts { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestAsserts?.Invoke(request);
                var content = request.Content as MultipartFormDataContent;
                Assert.IsType<MultipartFormDataContent>(content);
                Assert.NotNull(Asserts);

                await Asserts(content);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}
