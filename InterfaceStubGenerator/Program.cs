using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refit.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var generator = new InterfaceStubGenerator();
            var target = new FileInfo(args[0]);
            var files = args[1].Split(';').Select(x => new FileInfo(x)).ToArray();

            var template = generator.GenerateInterfaceStubs(files.Select(x => x.FullName).ToArray());
            File.WriteAllText(target.FullName, template);
        }
    }
}
