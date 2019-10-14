using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DependencyFinder.Search
{
    public class AsyncFileFinder
    {
        /// <summary>
        /// Fast version of filesearch (standard search took 22 seconds of c:\ while this 8s)
        /// https://github.com/dotnet/corefxlab/blob/31d98a89d2e38f786303bf1e9f8ba4cf5b203b0f/src/System.Threading.Tasks.Channels/README.md#example-producerconsumer-patterns
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchPattern"></param>
        /// <param name="workersNumber"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IAsyncEnumerable<string> Find(string path, string searchPattern, int workersNumber = 10, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path)) throw new ArgumentException($"Directory '{path}' doesn't exists");

            var dirs = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
            var files = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            EnumerateFiles(path, searchPattern, files);
            EnumerateDirs(dirs, path);

            var workers = new List<Task>();
            for (int i = 0; i < workersNumber; i++)
            {
                workers.Add(new Task(async () =>
                {
                    if (await dirs.Reader.WaitToReadAsync(cancellationToken)) //When this return false channel is empty
                    {
                        while (dirs.Reader.TryRead(out string subdir))
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            EnumerateFiles(subdir, searchPattern, files);

                            EnumerateDirs(dirs, subdir);
                        }
                    }
                }));
            }

            workers.ForEach(t => t.Start());
            Task.WhenAll(workers).ContinueWith(x => files.Writer.Complete());

            return files.Reader.ReadAllAsync();
        }

        private void EnumerateDirs(Channel<string> dirs, string subdir)
        {
            try
            {
                var sw = new SpinWait();
                foreach (var dir in Directory.EnumerateDirectories(subdir))
                {
                    while (!dirs.Writer.TryWrite(dir)) sw.SpinOnce();
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void EnumerateFiles(string dir, string searchPattern, Channel<string> channel)
        {
            try
            {
                var sw = new SpinWait();
                //foreach (var file in Directory.EnumerateFiles(dir, searchPattern))
                foreach (var file in FastFileInfo.EnumerateFiles(dir, searchPattern))
                {
                    while (!channel.Writer.TryWrite(file.FullName)) sw.SpinOnce();
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}