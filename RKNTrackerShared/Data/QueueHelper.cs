/*
Demoder.Common
Copyright (c) 2010,2011,2012 Demoder <demoder@demoder.me>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace RKNTracker.Tools
{
    /// <summary>
    /// Provides a queue with a single dedicated worker thread for its work items.
    /// </summary>
    /// <typeparam name="T">Type of items to be enqueued</typeparam>
    public class QueueHelper<T> : IDisposable
    {
        private BlockingCollection<T> incEventQueue = new BlockingCollection<T>();
        private QueueHelperDelegate<T> reportDelegate = null;
        protected Thread incEventThread;
        private bool disposed = false;

        /// <summary>
        /// Initializes this queue helper.
        /// </summary>
        /// <param name="reportDelegate">Method to handle incoming chat events.</param>
        public QueueHelper(QueueHelperDelegate<T> reportDelegate = null)
        {
            if (reportDelegate != null)
            {
                this.SetReportDelegate(reportDelegate);
            }
        }

        /// <summary>
        /// Defines a delegate which will be executed on the worker thread for each item in the queue.
        /// </summary>
        /// <param name="reportDelegate"></param>
        protected void SetReportDelegate(QueueHelperDelegate<T> reportDelegate, string tag = "")
        {
            if (this.disposed) { throw new ObjectDisposedException("QueueHelper"); }
            if (this.reportDelegate != null)
            {

            }
            this.reportDelegate = reportDelegate;

            this.incEventThread = new Thread(new ThreadStart(this.EventPuller));
            this.incEventThread.IsBackground = true;
            this.incEventThread.Name = "HandsoffHelper->EventPuller() (" + tag + ")";
            this.incEventThread.Start();
        }

        /// <summary>
        /// Add item to queue
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            if (this.disposed) { throw new ObjectDisposedException("QueueHelper"); }
            this.incEventQueue.Add(item);
        }

        #region private methods
        /// <summary>
        /// Dedicated worker for handling queue items.
        /// It'll execute the defined <see cref="reportDelegate"/>.
        /// </summary>
        /// <remarks>
        /// If the delegate throws an exception, it will be reported to the logging API.
        /// No attempt will be made to re-process the affected item. The thread will however 
        /// continue handling the queue. 
        /// </remarks>
        private void EventPuller()
        {
            try
            {
                while (!this.disposed)
                {
                    try
                    {
                        foreach (T item in this.incEventQueue.GetConsumingEnumerable())
                        {
                            if (this.disposed) { break; }
                            var delg = this.reportDelegate;
                            if (delg == null || delg.Target == null) { continue; }
                            this.reportDelegate.Invoke(item);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            finally
            {
            }
        }
        #endregion

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managed)
        {
            if (!managed) { return; }
            this.disposed = true;
            this.incEventQueue.Dispose();
            this.reportDelegate = null;
        }
    }
}
