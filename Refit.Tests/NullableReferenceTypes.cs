#nullable enable

using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this

namespace Refit.Tests
{
    interface IGenericWithResultService
    {
        [Get("/")]
        Task<string?> Get();
    }

    interface IGenericWithNullableValueService
    {
        [Get("/")]
        Task<int?> Get();
    }

    interface IGenericNullableReferenceService
    {
        [Get("/")]
        Task<string>? Get();
    }

    interface IGenericNullableValueService
    {
        [Get("/")]
        ValueTask<int>? Get();
    }

    interface IGenericNullableWithNullableReferenceService
    {
        [Get("/")]
        Task<string?>? Get();
    }

    interface IGenericNullableWithNullableValueService
    {
        [Get("/")]
        ValueTask<int?>? Get();
    }

    interface INullableReferenceService
    {
        [Get("/")]
        string? Get();
    }

    interface INullableValueService
    {
        [Get("/")]
        int? Get();
    }

    interface IReferenceAndValueParametersService
    {
        [Get("/")]
        Task Get(string? reference, int? value);
    }

    interface IGenericNullableReferenceParameterService
    {
        [Get("/")]
        Task Get(System.Collections.Generic.List<string>? reference);
    }

    interface IGenericWithNullableReferenceParameterService
    {
        [Get("/")]
        Task Get(System.Collections.Generic.List<string?> reference);
    }

    interface IGenericNullableWithNullableReferenceParameterService
    {
        [Get("/")]
        Task Get(System.Collections.Generic.List<string?>? reference);
    }

    interface ICustomNullableReferenceService
    {
        [Get("/")]
        CustomReferenceType? Get();
    }

    interface ICustomNullableValueService
    {
        [Get("/")]
        CustomValueType? Get();
    }

    interface ICustomReferenceAndValueParametersService
    {
        [Get("/")]
        Task Get(CustomReferenceType? reference, CustomValueType? value);
    }

    class CustomReferenceType { }
    class CustomValueType { }
}
