using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Refit.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            
            // NB: @Compile passes us a list of files relative to the project
            // directory - pass in the project and use its dir 
            var generator = new InterfaceStubGenerator();
            var target = new FileInfo(args[0]);
            var targetDir = new DirectoryInfo(args[1]);

            var files = default(FileInfo[]);

            if (args.Length > 2) {
                // We get a file with each line being a file
                files = File.ReadLines(args[2])
                    .Select(x => new FileInfo(Path.Combine(targetDir.FullName, x)))
                    .Where(x => x.Name.Contains("RefitStubs") == false && x.Exists && x.Length > 0)
                    .ToArray();
            } else {
                // NB: @Compile is completely jacked on Xam Studio in iOS, just
                // run down all of the .cs files in the current directory and hope
                // for the best
                files = recursivelyListFiles(targetDir, "*.cs").ToArray();
            }

            var template = generator.GenerateInterfaceStubs(files.Select(x => x.FullName).ToArray()).Trim();

            string contents = null;

            if (target.Exists) {
                // Only try writing if the contents are different. Don't cause a rebuild
                contents = File.ReadAllText(target.FullName, Encoding.UTF8).Trim();
                if (string.Equals(contents, template, StringComparison.Ordinal)) {
                    return;
                }    
            }
            
            
            // If the file is read-only, we might be on a build server. Check the file to see if 
            // the contents match what we expect
            if (target.Exists && target.IsReadOnly) {
                if (!string.Equals(contents, template, StringComparison.Ordinal)) {
                    Console.Error.WriteLine(new ReadOnlyFileError(target));
                    Environment.Exit(-1); // error....
                }
            } else {
                var retryCount = 3;

            retry:
                var file = default(FileStream);

                // NB: Parallel build weirdness means that we might get >1 person 
                // trying to party on this file at the same time.
                try {
                    file = File.Open(target.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
                } catch (Exception ex) {
                    if (retryCount < 0) {
                        throw;
                    }

                    retryCount--;
                    Thread.Sleep(500);
                    goto retry;
                }

                using(var sw = new StreamWriter(file, Encoding.UTF8)) {
                    sw.WriteLine(template);
                }
            }
        }

        static IEnumerable<FileInfo> recursivelyListFiles(DirectoryInfo root, string filter)
        {
            return root.GetFiles(filter)
                .Concat(root.GetDirectories()
                .SelectMany(x => recursivelyListFiles(x, filter)));
        }
    }

    static class ConcatExtension
    {
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> This, params IEnumerable<T>[] others)
        {
            foreach (var t in This) {
                yield return t;
            }

            foreach (var list in others) {
                foreach (var t in list) {
                    yield return t;
                }
            }
        }
    }
}
