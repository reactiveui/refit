using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Refit;

interface IServiceWithoutNamespace
{
    [Get("/")]
    Task GetRoot();

    [Post("/")]
    Task PostRoot();
}
