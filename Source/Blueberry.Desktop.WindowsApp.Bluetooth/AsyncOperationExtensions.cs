
using System.Threading.Tasks;
using Windows.Foundation;

namespace Blueberry.Desktop.WindowsApp.Bluetooth
{
    /// <summary>
    /// Provide helper methods for the <see cref="IAsyncOperation"/>
    /// </summary>
    public static class AsyncOperationExtensions
    {
        #region Constructor

        /// <summary>
        /// convert an <see cref="IAsyncOperation{TResult}"/>
        /// into a <see cref="Task{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">the type of result expected</typeparam>
        /// <param name="operation">the async operation</param>
        /// <returns></returns>
        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> operation)
        {
            // generate task completion result
            var tcs = new TaskCompletionSource<TResult>();

            // when the operation is completed...
            operation.Completed += delegate
            {
                switch(operation.Status)
                {
                    // if successful
                    case AsyncStatus.Completed:
                        // set result
                        tcs.TrySetResult(operation.GetResults());
                        break;
                    // if unsuccessful
                    case AsyncStatus.Error:
                        // set exception
                        tcs.TrySetException(operation.ErrorCode);
                        break;
                    // if canceled
                    case AsyncStatus.Canceled:
                        // set task as canceled
                        tcs.SetCanceled();
                        break;
                }
            };

            // return the task 
            return tcs.Task;
        }

        #endregion
    }
}
