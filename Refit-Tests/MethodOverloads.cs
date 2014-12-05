using System;
using System.Threading.Tasks;
using Refit;

namespace Refit.Tests
{
    public interface IUseOverloadedMethods
    {
        [Get("/")]
        Task Get();

        [Get("/{id}")]
        Task Get(int id);
    }
}

