using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Utilities
{

    public delegate void TimerCallback(object state);

    public sealed class Timer : CancellationTokenSource, IDisposable
    {

        private TimerCallback _callback;
        private object _state;
        private int _dueTimeInMilliseconds;

        public Timer(TimerCallback callback, object state, int dueTimeInMilliseconds)
        {
            _callback = callback;
            _state = state;
            _dueTimeInMilliseconds = dueTimeInMilliseconds;

            this.Reset();
        }

        public void Reset()
        {
            Task.Delay(_dueTimeInMilliseconds, Token)
                .ContinueWith(
                    (t, s) =>
                        {
                            var tuple = (Tuple<TimerCallback, object>)s;
                            tuple.Item1(tuple.Item2);
                        },
                    Tuple.Create(_callback, _state),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                    TaskScheduler.Default);
        }

        public new void Dispose() { base.Cancel(); }

    }

}
