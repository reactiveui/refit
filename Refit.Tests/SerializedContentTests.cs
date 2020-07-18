using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.Threading;

namespace Refit.Tests
{
    public class SerializedContentTests
    {
        const string BaseAddress = "https://api/";

        [Theory]
        [InlineData(typeof(NewtonsoftJsonContentSerializer))]
        [InlineData(typeof(SystemTextJsonContentSerializer))]
        [InlineData(typeof(XmlContentSerializer))]
        public async Task WhenARequestRequiresABodyThenItDoesNotDeadlock(Type contentSerializerType)
        {
            if (!(Activator.CreateInstance(contentSerializerType) is IContentSerializer serializer))
            {
                throw new ArgumentException($"{contentSerializerType.FullName} does not implement {nameof(IContentSerializer)}");
            }

            var handler = new MockPushStreamContentHttpMessageHandler
            {
                Asserts = async content => new StringContent(await content.ReadAsStringAsync().ConfigureAwait(false))
            };

            var settings = new RefitSettings(serializer)
            {
                HttpMessageHandlerFactory = () => handler
            };

            var fixture = RestService.For<IGitHubApi>(BaseAddress, settings);

            var fixtureTask = await RunTaskWithATimeLimit(fixture.CreateUser(new User())).ConfigureAwait(false);
            Assert.True(fixtureTask.IsCompleted);
            Assert.Equal(TaskStatus.RanToCompletion, fixtureTask.Status);
        }

        [Theory]
        [InlineData(typeof(NewtonsoftJsonContentSerializer))]
        [InlineData(typeof(SystemTextJsonContentSerializer))]
        [InlineData(typeof(XmlContentSerializer))]
        public async Task WhenARequestRequiresABodyThenItIsSerialized(Type contentSerializerType)
        {
            if (!(Activator.CreateInstance(contentSerializerType) is IContentSerializer serializer))
            {
                throw new ArgumentException($"{contentSerializerType.FullName} does not implement {nameof(IContentSerializer)}");
            }

            var model = new User
            {
                Name = "Wile E. Coyote",
                CreatedAt = new DateTime(1949, 9, 16).ToString(),
                Company = "ACME",
            };

            var handler = new MockPushStreamContentHttpMessageHandler
            {
                Asserts = async content =>
                {
                    var stringContent = new StringContent(await content.ReadAsStringAsync().ConfigureAwait(false));
                    var user = await serializer.DeserializeAsync<User>(content).ConfigureAwait(false);
                    Assert.NotSame(model, user);
                    Assert.Equal(model.Name, user.Name);
                    Assert.Equal(model.CreatedAt, user.CreatedAt);
                    Assert.Equal(model.Company, user.Company);

                    //  return some content so that the serializer doesn't complain
                    return stringContent;
                }
            };

            var settings = new RefitSettings(serializer)
            {
                HttpMessageHandlerFactory = () => handler
            };

            var fixture = RestService.For<IGitHubApi>(BaseAddress, settings);

            var fixtureTask = await RunTaskWithATimeLimit(fixture.CreateUser(model)).ConfigureAwait(false);

            Assert.True(fixtureTask.IsCompleted);
        }

        [Fact]
        public void VerityDefaultSerializer()
        {
            var settings = new RefitSettings();

            Assert.NotNull(settings.ContentSerializer);
            Assert.IsType<NewtonsoftJsonContentSerializer>(settings.ContentSerializer);

            settings = new RefitSettings(new SystemTextJsonContentSerializer());

            Assert.NotNull(settings.ContentSerializer);
            Assert.IsType<SystemTextJsonContentSerializer>(settings.ContentSerializer);
        }

        /// <summary>
        /// Runs the task to completion or until the timeout occurs
        /// </summary>
        static async Task<Task<User>> RunTaskWithATimeLimit(Task<User> fixtureTask)
        {
            var circuitBreakerTask = Task.Delay(TimeSpan.FromSeconds(30));
            await Task.WhenAny(fixtureTask, circuitBreakerTask);
            return fixtureTask;
        }

        class MockPushStreamContentHttpMessageHandler : HttpMessageHandler
        {
            public Func<PushStreamContent, Task<HttpContent>> Asserts { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var content = request.Content as PushStreamContent;
                Assert.IsType<PushStreamContent>(content);
                Assert.NotNull(Asserts);

                var responseContent = await Asserts(content).ConfigureAwait(false);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };
            }
        }

        [Fact]
        public async Task StreamDeserialization_UsingSystemTextJsonContentSerializer()
        {
            var model = new TestAliasObject
            {
                ShortNameForAlias = nameof(StreamDeserialization_UsingSystemTextJsonContentSerializer),
                ShortNameForJsonProperty = nameof(TestAliasObject)
            };

            var serializer = new SystemTextJsonContentSerializer();

            var json = await serializer.SerializeAsync(model);

            var stream = await json.ReadAsStreamAsync();

            var result = await System.Text.Json.JsonSerializer.DeserializeAsync<TestAliasObject>(stream);

            Assert.NotNull(result);
            Assert.Equal(model.ShortNameForAlias, result.ShortNameForAlias);
            Assert.Equal(model.ShortNameForJsonProperty, result.ShortNameForJsonProperty);
        }

        [Fact]
        public async Task StreamDeserialization_UsingSystemTextJsonContentSerializer_SetsCorrectHeaders()
        {
            var model = new TestAliasObject
            {
                ShortNameForAlias = nameof(StreamDeserialization_UsingSystemTextJsonContentSerializer),
                ShortNameForJsonProperty = nameof(TestAliasObject)
            };

            var serializer = new SystemTextJsonContentSerializer();

            var json = await serializer.SerializeAsync(model);

            Assert.NotNull(json.Headers.ContentLength);
            Assert.True(json.Headers.ContentLength.Value > 0);
            Assert.NotNull(json.Headers.ContentType);
            Assert.Equal("utf-8", json.Headers.ContentType.CharSet);
            Assert.Equal("application/json", json.Headers.ContentType.MediaType);
        }
    }
}
