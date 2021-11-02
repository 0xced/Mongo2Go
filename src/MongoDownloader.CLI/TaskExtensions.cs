using System;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDownloader.CLI
{
    internal static class TaskExtensions
    {
        public static async Task<TResult> Combine<T1, T2, T3, TResult>(this Task<T1> task1, Func<T1, T2, T3, CancellationToken, Task<TResult>> task2, T2 arg2, T3 arg3, CancellationToken cancellationToken)
        {
            return await task2(await task1, arg2, arg3, cancellationToken);
        }
    }
}