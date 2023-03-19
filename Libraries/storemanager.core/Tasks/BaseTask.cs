/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VDS.RDF.Utilities.StoreManager.Tasks
{
    /// <summary>
    /// Abstract Base Class for Tasks
    /// </summary>
    /// <typeparam name="TResult">Task Result Type</typeparam>
    public abstract class BaseTask<TResult>
        : ITask<TResult> where TResult : class
    {
        private TaskState _state = TaskState.Unknown;
        private string _information;
        private readonly RunTaskInternalDelegate _delegate;
        private Exception _error;
        private TaskCallback<TResult> _callback;
        private DateTime? _start = null, _end = null;

        /// <summary>
        /// Creates a new Base Task
        /// </summary>
        /// <param name="name">Task Name</param>
        protected BaseTask(string name)
        {
            Name = name;
            _state = TaskState.NotRun;
            _delegate = new BaseTask<TResult>.RunTaskInternalDelegate(RunTaskInternal);
        }

        /// <summary>
        /// Gets/Sets the Task State
        /// </summary>
        public TaskState State
        {
            get { return _state; }
            protected set
            {
                _state = value;
                RaiseStateChanged();
            }
        }

        /// <summary>
        /// Gets the Task Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets/Sets the Task Information
        /// </summary>
        public string Information
        {
            get { return _information; }
            set
            {
                _information = value;
                RaiseStateChanged();
            }
        }

        /// <summary>
        /// Gets/Sets the Task Error (if any)
        /// </summary>
        public Exception Error
        {
            get { return _error; }
            protected set
            {
                _error = value;
                RaiseStateChanged();
            }
        }

        /// <summary>
        /// Gets the Elapsed Time (if known)
        /// </summary>
        public TimeSpan? Elapsed
        {
            get
            {
                if (_start == null) return null;
                if (_end != null) return (_end.Value - _start.Value);
                return (DateTime.Now - _start.Value);
            }
        }

        /// <summary>
        /// Gets the Task Result
        /// </summary>
        public TResult Result { get; private set; }

        /// <summary>
        /// Runs the Task
        /// </summary>
        /// <param name="callback">Callback</param>
        public void RunTask(TaskCallback<TResult> callback)
        {
            _start = DateTime.Now;
            _callback = callback;
            State = TaskState.Starting;
            _delegate.BeginInvoke(new AsyncCallback(CompleteTask), null);
            State = TaskState.Running;
        }

        /// <summary>
        /// Delegate for running tasks in the background
        /// </summary>
        /// <returns></returns>
        private delegate TResult RunTaskInternalDelegate();

        /// <summary>
        /// Abstract method to be implemented by derived classes to implement the actual logic of the task
        /// </summary>
        /// <returns></returns>
        protected abstract TResult RunTaskInternal();

        /// <summary>
        /// Complets the task invoking the callback appropriately
        /// </summary>
        /// <param name="result">Result</param>
        private void CompleteTask(IAsyncResult result)
        {
            try
            {
                //End the Invoke saving the Result
                Result = _delegate.EndInvoke(result);
                State = TaskState.Completed;
            }
            catch (Exception ex)
            {
                //Invoke errored so save the Error
                State = TaskState.CompletedWithErrors;
                Information = "Error - " + ex.Message;
                Error = ex;
            }
            finally
            {
                _end = DateTime.Now;
                //Invoke the Callback
                _callback(this);
            }
        }

        /// <summary>
        /// Gets whether the Task may be cancelled
        /// </summary>
        public abstract bool IsCancellable { get; }

        /// <summary>
        /// Cancels the Task
        /// </summary>
        public abstract void Cancel();

        /// <summary>
        /// Event raised when the Task State changes
        /// </summary>
        public event TaskStateChanged StateChanged;

        /// <summary>
        /// Helper for raising the Task State changed event
        /// </summary>
        protected void RaiseStateChanged()
        {
            TaskStateChanged d = StateChanged;
            if (d != null)
            {
                d();
            }
        }
    }

    /// <summary>
    /// Abstract Base Class for cancellable Tasks
    /// </summary>
    /// <typeparam name="T">Task Result Type</typeparam>
    public abstract class CancellableTask<T>
        : BaseTask<T> where T : class
    {
        /// <summary>
        /// Creates a new Task
        /// </summary>
        /// <param name="name">Task Name</param>
        protected CancellableTask(string name)
            : base(name)
        {
            HasBeenCancelled = false;
        }

        /// <summary>
        /// Gets whether the Task is cancellable, true unless the task has already completed
        /// </summary>
        public override sealed bool IsCancellable
        {
            get { return State != TaskState.Completed && State != TaskState.CompletedWithErrors; }
        }

        /// <summary>
        /// Cancels the task
        /// </summary>
        public override sealed void Cancel()
        {
            HasBeenCancelled = true;
            State = TaskState.RunningCancelled;
            CancelInternal();
        }

        /// <summary>
        /// Gets whether we've been told to cancel
        /// </summary>
        public bool HasBeenCancelled { get; private set; }

        /// <summary>
        /// Virtual method that derived classes may override if they wish to take action as soon as we are told to cancel
        /// </summary>
        protected virtual void CancelInternal()
        {
        }
    }

    /// <summary>
    /// Abstract Base Class for non-cancellable Tasks
    /// </summary>
    /// <typeparam name="T">Task Result Type</typeparam>
    public abstract class NonCancellableTask<T>
        : BaseTask<T> where T : class
    {
        /// <summary>
        /// Creates a new Task
        /// </summary>
        /// <param name="name">Name</param>
        protected NonCancellableTask(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Returns that the Task may not be cancelled
        /// </summary>
        public override bool IsCancellable
        {
            get { return false; }
        }

        /// <summary>
        /// Has no effect since the Task may not be cancelled
        /// </summary>
        public override sealed void Cancel()
        {
            //Does Nothing
        }
    }
}