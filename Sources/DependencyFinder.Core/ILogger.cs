using System;

namespace DependencyFinder.Core
{
    public interface ILogger
    {
        public void LogError(Exception error);

        public event Action<Exception> Error;
    }

    public class SimpleLogger : ILogger
    {
        public event Action<Exception> Error;

        public void LogError(Exception error)
        {
            Error?.Invoke(error);
        }
    }

}