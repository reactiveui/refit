using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Refit.Generator.App
{
    class Program
    {
        static int Main(string[] args)
        {
            // NB: @Compile passes us a list of files relative to the project
            // directory - pass in the project and use its dir 

            string refitInternalNamespace = null;
            if (args.Length >= 4)
            {
                refitInternalNamespace = args[3];
            }

            var generator = new InterfaceStubGenerator(refitInternalNamespace, msg => Console.Out.WriteLine(msg));
            var target = new FileInfo(args[0]);
            var targetDir = new DirectoryInfo(args[1]);

            var files = default(FileInfo[]);

            if (args.Length >= 3)
            {
                // We get a file with each line being a file
                files = File.ReadLines(args[2])
                            .Distinct()
                            .Select(x => File.Exists(x) ? new FileInfo(x) : new FileInfo(Path.Combine(targetDir.FullName, x)))
                            .Where(x => x.Name.Contains("RefitStubs") == false && x.Exists && x.Length > 0)
                            .ToArray();
            }
            else
            {
                return -1;
            }

            var template = generator.GenerateInterfaceStubs(files.Select(x => x.FullName).ToArray()).Trim();

            string contents = null;

            if (target.Exists)
            {
                // Only try writing if the contents are different. Don't cause a rebuild
                contents = File.ReadAllText(target.FullName, Encoding.UTF8).Trim();
                if (string.Equals(contents, template, StringComparison.Ordinal))
                {
                    return 0;
                }
            }


            // If the file is read-only, we might be on a build server. Check the file to see if 
            // the contents match what we expect
            if (target.Exists && target.IsReadOnly)
            {
                if (!string.Equals(contents, template, StringComparison.Ordinal))
                {
                    Console.Error.WriteLine(new ReadOnlyFileError(target));
                    return -1; // error....
                }
            }
            else
            {
                var retryCount = 3;

retry:
                var file = default(FileStream);

                // NB: Parallel build weirdness means that we might get >1 person 
                // trying to party on this file at the same time.
                try
                {
                    file = File.Open(target.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
                }
                catch (Exception)
                {
                    if (retryCount < 0)
                    {
                        throw;
                    }

                    retryCount--;
                    Thread.Sleep(500);
                    goto retry;
                }

                using (var sw = new StreamWriter(file, Encoding.UTF8))
                {
                    sw.WriteLine(template);
                }
            }

            return 0;
        }
    }

    static class ConcatExtension
    {
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> @this, params IEnumerable<T>[] others)
        {
            foreach (var t in @this)
            {
                yield return t;
            }

            foreach (var list in others)
            {
                foreach (var t in list)
                {
                    yield return t;
                }
            }
        }
    }
}
