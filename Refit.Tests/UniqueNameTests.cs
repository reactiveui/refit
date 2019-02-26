using System.Collections.Generic;
using Xunit;

namespace Refit.Tests
{
    namespace Http
    {
        class Client
        {
            public class Request { }

            public class Response { }
        }
    }

    namespace Tcp
    {
        class Client { }
    }

    public class UniqueNameTests
    {
        [Fact]
        public void SystemTypeAndLanguageTypeHaveSameNames()
        {
            var name1 = UniqueName.ForType<System.Int32>();
            var name2 = UniqueName.ForType<int>();

            Assert.Equal(name1, name2);
        }

        [Fact]
        public void GenericClassWithDifferentTypesHaveUniqueNames()
        {
            var name1 = UniqueName.ForType<List<long>>();
            var name2 = UniqueName.ForType<List<int>>();

            Assert.NotEqual(name1, name2);
        }

        [Fact]
        public void SameClassNameInDifferentNamespacesHaveUniqueNames()
        {
            var name1 = UniqueName.ForType<Http.Client>();
            var name2 = UniqueName.ForType<Tcp.Client>();

            Assert.NotEqual(name1, name2);
        }

        [Fact]
        public void ClassesWithNestedClassesHaveUniqueNames()
        {
            var name1 = UniqueName.ForType<Http.Client>();
            var name2 = UniqueName.ForType<Http.Client.Request>();

            Assert.NotEqual(name1, name2);
        }

        [Fact]
        public void NestedClassesHaveUniqueNames()
        {
            var name1 = UniqueName.ForType<Http.Client.Request>();
            var name2 = UniqueName.ForType<Http.Client.Response>();

            Assert.NotEqual(name1, name2);
        }
    }
}
