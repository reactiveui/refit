using System.ComponentModel;
using System.Threading.Tasks;
using Refit;

interface IFooWithOtherAttribute
{
    [Get("/")]
    Task GetRoot();

    [System.ComponentModel.DisplayName("/")]
    Task PostRoot();
}
