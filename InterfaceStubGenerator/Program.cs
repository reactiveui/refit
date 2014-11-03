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
            // directory - we're going to assume that the target is always in
            // the same directory as the project file
            var generator = new InterfaceStubGenerator();
            var target = new FileInfo(args[0]);
            var targetDir = target.DirectoryName;

            var files = default(FileInfo[]);

            if (args.Length > 1) {
                files = args[1].Split(';')
                    .Select(x => new FileInfo(Path.Combine(targetDir, x)))
                    .Where(x => x.Name.Contains("RefitStubs") == false && x.Exists && x.Length > 0)
                    .ToArray();
            } else {
                // NB: @Compile is completely jacked on Xam Studio in iOS, just
                // run down all of the .cs files in the current directory and hope
                // for the best
                files = recursivelyListFiles(target.Directory, "*.cs").ToArray();
            }

            var template = generator.GenerateInterfaceStubs(files.Select(x => x.FullName).ToArray());

        int retryCount = 3; 
        retry:
            var file = default(FileStream);

            // NB: Parallel build weirdness means that we might get >1 person 
            // trying to party on this file at the same time.
            try {
                file = File.Open(target.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
            } catch (Exception ex) {
                if (retryCount < 0) throw;

                retryCount--;
                Thread.Sleep(500);
                goto retry;
            }

            using (var sw = new StreamWriter(file, Encoding.UTF8)) {
                sw.WriteLine(template);
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
