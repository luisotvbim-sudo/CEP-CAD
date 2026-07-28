using System;
using System.Windows.Threading;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DeferredUiAction
    {
        private readonly Dispatcher _dispatcher;
        private readonly DispatcherPriority _priority;
        private bool _isScheduled;

        public DeferredUiAction(
            Dispatcher dispatcher,
            DispatcherPriority priority)
        {
            _dispatcher = dispatcher ??
                throw new ArgumentNullException(nameof(dispatcher));
            _priority = priority;
        }

        public void Schedule(Action action)
        {
            if (action == null || _isScheduled)
            {
                return;
            }

            _isScheduled = true;
            _dispatcher.BeginInvoke(
                _priority,
                new Action(() =>
                {
                    try
                    {
                        action();
                    }
                    finally
                    {
                        _isScheduled = false;
                    }
                }));
        }
    }
}
