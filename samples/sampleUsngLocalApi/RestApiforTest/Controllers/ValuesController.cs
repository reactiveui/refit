using System.Collections.Generic;
using LibraryWithSDKandRefitService;
using Microsoft.AspNetCore.Mvc;

namespace RestApiforTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "Get Api with no argument was Called";
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "Get Api was called";
        }

        // POST api/values
        [HttpPost]
        public ActionResult<string> Post([FromBody] ModelForTest testObject)
        {
            return "Post Api was Called";
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public ActionResult<string> Put(int id, [FromBody] ModelForTest testObject)
        {
            return "Put Api was called";
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public ActionResult<string> Delete(int id)
        {
            return "Delete Api was Called";
        }
    }
}
