using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using PclSocketException = Sockets.Plugin.Abstractions.SocketException;
using PlatformSocketException = System.Net.Sockets.SocketException;

namespace Sockets.Plugin.Abstractions
{
    public static class NativeExceptionExtensions
    {
        internal static readonly HashSet<Type> NativeSocketExceptions = new HashSet<Type> { typeof(PlatformSocketException) };

        public static Task WrapNativeSocketExceptions(this Task task)
        {
            return task.ContinueWith(
                t =>
                {

                    if (!t.IsFaulted)
                        return t;

                    var ex = t.Exception?.InnerException ?? t.Exception;

                    throw (NativeSocketExceptions.Contains(ex.GetType()))
                        ? new PclSocketException(ex)
                        : ex;
                });
        }

        public static Task<T> WrapNativeSocketExceptions<T>(this Task<T> task)
        {
            return task.ContinueWith<T>(
                t =>
                {
                    if (!t.IsFaulted)
                        return t.Result;
                    Console.WriteLine("exception");
                    var ex = t.Exception?.InnerException ?? t.Exception;
                    Console.WriteLine(ex.ToString());
                    //throw (NativeSocketExceptions.Contains(ex.GetType()))
                    //    ? new PclSocketException(ex)
                    //    : ex;
                    return default(T);
                });
        }
    }
}
