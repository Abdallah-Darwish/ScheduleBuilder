using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ScheduleBuilder.Core
{
    /// <summary>
    /// Async and maybe faster version of <see cref="AutoResetEvent"/>
    /// </summary>
    /// <remarks>Source : https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-2-asyncautoresetevent/ </remarks>
    public class AsyncAutoResetEvent
    {
        private static readonly Task s_completed = Task.CompletedTask;
        private readonly Queue<TaskCompletionSource<bool>> _waits = new Queue<TaskCompletionSource<bool>>();
        private bool _signaled;
        public AsyncAutoResetEvent(bool initialState = true) => _signaled = initialState;
        public Task WaitAsync()
        {
            lock (_waits)
            {
                if (_signaled)
                {
                    _signaled = false;
                    return s_completed;
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    _waits.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;

            lock (_waits)
            {
                if (_waits.Count > 0)
                    toRelease = _waits.Dequeue();
                else if (!_signaled)
                    _signaled = true;
            }
            
            toRelease?.SetResult(true);
        }
    }
}
