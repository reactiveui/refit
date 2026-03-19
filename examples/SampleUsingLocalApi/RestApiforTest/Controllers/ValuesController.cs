using LibraryWithSDKandRefitService;

using Microsoft.AspNetCore.Mvc;

namespace RestApiforTest.Controllers;

/// <summary>
/// Represents an API controller that provides endpoints for managing and retrieving values.
/// </summary>
/// <remarks>This controller defines standard HTTP endpoints for GET, POST, PUT, and DELETE operations
/// using RESTful conventions. Each action corresponds to a specific HTTP verb and route, allowing clients to
/// interact with value resources. The controller is intended for demonstration or template purposes and can be
/// extended to implement actual data operations.</remarks>
[Route("api/[controller]")]
[ApiController]
public class ValuesController : ControllerBase
{
    /// <summary>
    /// Handles HTTP GET requests and returns a confirmation message indicating that the API was called without
    /// arguments.
    /// </summary>
    /// <returns>An <see cref="ActionResult{String}"/> containing a message that the API was called with no arguments.</returns>
    [HttpGet]
    public ActionResult<string> Get()
    {
        return "Get Api with no argument was Called";
    }

    /// <summary>
    /// Retrieves a string response for the specified identifier.
    /// </summary>
    /// <param name="id">The identifier for which to retrieve the response.</param>
    /// <returns>An <see cref="ActionResult{String}"/> containing the response string for the specified identifier.</returns>
    [HttpGet("{id}")]
    public ActionResult<string> Get(int id)
    {
        return "Get Api was called";
    }

    /// <summary>
    /// Handles HTTP POST requests to create a new resource using the provided test object.
    /// </summary>
    /// <param name="testObject">The object containing the data to be processed in the POST request. Cannot be null.</param>
    /// <returns>An ActionResult containing a string that indicates the result of the POST operation.</returns>
    [HttpPost]
    public ActionResult<string> Post([FromBody] ModelForTest testObject)
    {
        return "Post Api was Called";
    }

    /// <summary>
    /// Updates the resource identified by the specified ID with the provided data.
    /// </summary>
    /// <param name="id">The unique identifier of the resource to update.</param>
    /// <param name="testObject">The data used to update the resource. Cannot be null.</param>
    /// <returns>An ActionResult containing a confirmation message indicating that the update operation was called.</returns>
    [HttpPut("{id}")]
    public ActionResult<string> Put(int id, [FromBody] ModelForTest testObject)
    {
        return "Put Api was called";
    }

    /// <summary>
    /// Deletes the resource identified by the specified ID.
    /// </summary>
    /// <param name="id">The unique identifier of the resource to delete.</param>
    /// <returns>An ActionResult containing a message indicating that the delete operation was called.</returns>
    [HttpDelete("{id}")]
    public ActionResult<string> Delete(int id)
    {
        return "Delete Api was Called";
    }
}
