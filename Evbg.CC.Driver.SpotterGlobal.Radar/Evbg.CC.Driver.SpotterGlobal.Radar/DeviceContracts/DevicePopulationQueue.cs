using log4net;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar
{
    public class DevicePopulationQueue
    {
        private static readonly DevicePopulationQueue Queue = new DevicePopulationQueue();
        private readonly BlockingCollection<Func<Task>> _queue;
        private ILog _log;
        private bool _processingQueue;

        public static DevicePopulationQueue Instance(ILog log)
        {
            if (Queue._log == null)
            {
                Queue._log = log;
            }

            return Queue;
        }


        public event EventHandler Empty;

        private DevicePopulationQueue()
        {
            _queue = new BlockingCollection<Func<Task>>();
        }

        public void Enqueue(Func<Task> item)
        {
            if (!_queue.Contains(item))
            {
                _queue.Add(item);

                if (!_processingQueue)
                {
                    Task.Run(QueueProcessor);
                }
            }
        }

        protected virtual void OnEmpty(object sender, EventArgs args)
        {
            Empty?.Invoke(sender, args);
        }

        private async Task QueueProcessor()
        {
            _log.Debug("Start the process queue");
            _processingQueue = true;

            while (_queue.Count > 0)
            {
                //this call blocks the thread until there is a new item in the queue
                var item = _queue.Take();

                await item.Invoke();
            }

            _processingQueue = false;
            _log.Debug("Terminate the process queue");
            OnEmpty(this, EventArgs.Empty);
        }
    }
}