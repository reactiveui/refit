using System.Collections.Generic;

namespace Refit
{
    public class ProblemDetails
    {
        public Dictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
        public string Type { get; set; }
        public string Title { get; set; }
        public int Status { get; set; }
        public string Detail { get; set; }
        public string Instance { get; set; }
    }

}
