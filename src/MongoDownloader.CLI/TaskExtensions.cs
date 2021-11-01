using System;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDownloader.CLI
{
    internal static class TaskExtensions
    {
        public static async Task<TResult> Combine<T, T2, T3, TResult>(this Task<T> task, Func<T, T2, T3, CancellationToken, Task<TResult>> continuation, T2 arg2, T3 arg3, CancellationToken cancellationToken)
        {
            return await continuation(await task, arg2, arg3, cancellationToken);
        }
    }
}