using FastSearchLibrary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyFinder.Core
{
    public class SolutionSearcher
    {
        private readonly ConcurrentBag<FileInfo> _result = new ConcurrentBag<FileInfo>();
        private FileSearcher _searcher;

        public Task<IEnumerable<FileInfo>> Search(string rootDirectory, CancellationToken token = default)
        {
            var tcs = new TaskCompletionSource<IEnumerable<FileInfo>>();

            //TODO
            _searcher = new FileSearcher(rootDirectory, "*.dll", token);

            _searcher.FilesFound += (sender, arg) =>
            {
                foreach (var file in arg.Files)
                {
                    _result.Add(file);
                }
            };

            _searcher.SearchCompleted += (sender, arg) =>
            {
                if (arg.IsCanceled)
                {
                    tcs.SetResult(Enumerable.Empty<FileInfo>());
                }

                tcs.SetResult(_result.ToArray());
            };

            _searcher.StartSearch();

            return tcs.Task;
        }
    }
}