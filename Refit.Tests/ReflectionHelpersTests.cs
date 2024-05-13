using System;
using Xunit;
using Refit;

namespace Refit.Tests
{
    public class ReflectionHelpersTests
    {
        [Fact]
        public void NullTargetInterfaceThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ReflectionHelpers.GetPathPrefixFor(null));
        }

        [Fact]
        public void TargetInterfaceHasPathPrefixAttributeReturnsCorrectPathPrefix()
        {
            var pathPrefix = ReflectionHelpers.GetPathPrefixFor(typeof(IInterfaceWithPathPrefix));
            Assert.Equal("/pathPrefix", pathPrefix);
        }

        [Fact]
        public void InheritedInterfaceHasPathPrefixAttributeReturnsCorrectPathPrefix()
        {
            var pathPrefix = ReflectionHelpers.GetPathPrefixFor(typeof(IInterfaceInheritingPathPrefix));
            Assert.Equal("/pathPrefix", pathPrefix);
        }

        [Fact]
        public void NoPathPrefixAttributeReturnsEmptyString()
        {
            var pathPrefix = ReflectionHelpers.GetPathPrefixFor(typeof(IInterfaceWithoutPathPrefix));
            Assert.Equal(string.Empty, pathPrefix);
        }

        [PathPrefix("/pathPrefix")]
        public interface IInterfaceWithPathPrefix { }

        public interface IInterfaceInheritingPathPrefix : IInterfaceWithPathPrefix { }

        public interface IInterfaceWithoutPathPrefix { }
    }
}
