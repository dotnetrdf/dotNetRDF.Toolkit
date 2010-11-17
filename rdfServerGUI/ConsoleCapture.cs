﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace rdfServer.GUI
{
    class ConsoleCapture : IDisposable
    {
        private Process _process;
        private ServerMonitor _monitor;

        private DataReceivedEventHandler _errorHandler, _outputHandler;

        public ConsoleCapture(Process p, ServerMonitor monitor)
        {
            this._process = p;
            this._monitor = monitor;

            this._errorHandler = new DataReceivedEventHandler(this.HandleError);
            this._outputHandler = new DataReceivedEventHandler(this.HandleOutput);

            if (!this._process.StartInfo.UseShellExecute && this._process.StartInfo.RedirectStandardError && this._process.StartInfo.RedirectStandardOutput)
            {
                this._process.BeginErrorReadLine();
                this._process.BeginOutputReadLine();
                this._process.ErrorDataReceived += this._errorHandler;
                this._process.OutputDataReceived += this._outputHandler;
            }
            else
            {
                monitor.WriteLine("Cannot monitor a process that was not started in the current rdfServerGUI session but you may still stop this server");
            }
        }

        private void HandleError(Object sender, DataReceivedEventArgs args)
        {
            this._monitor.WriteLine(args.Data);
        }

        private void HandleOutput(Object sender, DataReceivedEventArgs args)
        {
            this._monitor.WriteLine(args.Data);
        }

        public void Dispose()
        {
            if (this._process != null)
            {
                if (!this._process.StartInfo.UseShellExecute && this._process.StartInfo.RedirectStandardError && this._process.StartInfo.RedirectStandardOutput)
                {
                    this._process.CancelErrorRead();
                    this._process.CancelOutputRead();
                    this._process.ErrorDataReceived -= this._errorHandler;
                    this._process.OutputDataReceived -= this._outputHandler;
                }
                this._process = null;
            }
        }
    }
}
