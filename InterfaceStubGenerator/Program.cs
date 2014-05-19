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
            // NB: @Compile passes us a list of files relative to the project
            // directory - we're going to assume that the target is always in
            // the same directory as the project file
            var generator = new InterfaceStubGenerator();
            var target = new FileInfo(args[0]);
            var targetDir = target.DirectoryName;

            var files = args[1].Split(';')
                .Select(x => new FileInfo(Path.Combine(targetDir, x)))
                .Where(x => x.Name.Contains("RefitStubs") == false)
                .ToArray();

            var template = generator.GenerateInterfaceStubs(files.Select(x => x.FullName).ToArray());
            using (var of = File.OpenWrite(target.FullName)) {
                var bytes = Encoding.UTF8.GetBytes(template);
                of.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
