using Dasync.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DependencyFinder.Search
{
    public class AsyncFileFinder
    {
        public IAsyncEnumerable<string> Find(string path, string searchPattern, IEnumerable<string> skipDirs, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path)) throw new ArgumentException($"Directory '{path}' doesn't exists");

            var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            foreach (var file in Directory.EnumerateFiles(path, searchPattern))
            {
                channel.Writer.TryWrite(file);
            }

            var result = SearchInDivectory(channel, path, searchPattern, skipDirs, cancellationToken);

            result.ContinueWith(task => channel.Writer.Complete());

            return channel.Reader.ReadAllAsync();
        }

        private async Task SearchInDivectory(Channel<string> channel, string path, string searchPattern, IEnumerable<string> skipDirs, CancellationToken cancellationToken)
        { 
            var subdirs = Directory.GetDirectories(path);//.Where(d => !skipDirs.Contains(d, StringComparer.OrdinalIgnoreCase));

            await subdirs.ParallelForEachAsync(async subdir =>
            {
                foreach (var file in Directory.EnumerateFiles(subdir, searchPattern))
                {
                    channel.Writer.TryWrite(file);
                }

                await SearchInDivectory(channel, subdir, searchPattern, skipDirs, cancellationToken);
            });
        }
    }
}