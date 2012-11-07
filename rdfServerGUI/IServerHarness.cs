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
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using VDS.Web;
using VDS.Web.Logging;

namespace VDS.RDF.Utilities.Server.GUI
{
    /// <summary>
    /// Represents a harness around a server that can be used to control and monitor the server
    /// </summary>
    interface IServerHarness
        : IDisposable
    {
        /// <summary>
        /// Stop the Server
        /// </summary>
        void Stop();

        /// <summary>
        /// Start the Server
        /// </summary>
        void Start();

        /// <summary>
        /// Get whether the Server is running
        /// </summary>
        bool IsRunning
        {
            get;
        }

        /// <summary>
        /// Gets whether pausing and resuming the server is supported
        /// </summary>
        bool CanPauseAndResume
        {
            get;
        }

        /// <summary>
        /// Pauses the server
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes the server
        /// </summary>
        void Resume();

        /// <summary>
        /// Attaches a Monitor to the Server
        /// </summary>
        /// <param name="monitor">Monitor</param>
        /// <remarks>
        /// Only one monitor is permitted at any one time
        /// </remarks>
        void AttachMonitor(ServerMonitor monitor);

        /// <summary>
        /// Detaches the current monitor from the Server
        /// </summary>
        void DetachMonitor();
    }

    /// <summary>
    /// Server Harness for in-process servers
    /// </summary>
    class InProcessServerHarness 
        : IServerHarness
    {
        private HttpServer _server;
        private MonitorLogger _activeLogger;
        private String _logFormat;

        /// <summary>
        /// Creates a new in-process harness
        /// </summary>
        /// <param name="server">Server</param>
        /// <param name="logFormat">Log Format</param>
        public InProcessServerHarness(HttpServer server, String logFormat)
        {
            this._server = server;
            this._logFormat = ApacheStyleLogger.GetLogFormat(logFormat);
        }

        #region IServerHarness Members

        /// <summary>
        /// Stops the Server
        /// </summary>
        public void Stop()
        {
            this._server.Stop();
        }

        /// <summary>
        /// Starts the Server
        /// </summary>
        public void Start()
        {
            this._server.Start();
        }

        /// <summary>
        /// Gets whether the Server is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this._server.IsRunning;
            }
        }

        /// <summary>
        /// Returns that the server may be paused and resumed
        /// </summary>
        public bool CanPauseAndResume
        {
            get 
            {
                return true; 
            }
        }

        /// <summary>
        /// Pauses the server
        /// </summary>
        public void Pause()
        {
            this.Stop();
        }

        /// <summary>
        /// Resumes the server
        /// </summary>
        public void Resume()
        {
            this.Start();
        }

        /// <summary>
        /// Attaches a Monitor to the Server
        /// </summary>
        /// <param name="monitor">Monitor</param>
        public void AttachMonitor(ServerMonitor monitor)
        {
            this.DetachMonitor();
            this._activeLogger = new MonitorLogger(monitor, this._logFormat);
            this._server.AddLogger(this._activeLogger);
        }

        /// <summary>
        /// Detaches the current monitor from the server
        /// </summary>
        public void DetachMonitor()
        {
            if (this._activeLogger != null)
            {
                this._server.RemoveLogger(this._activeLogger);
            }
        }

        #endregion

        /// <summary>
        /// Gets the String representation of the Server
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[In-Process] " + this._server.Host + ":" + this._server.Port + " - " + ((this.IsRunning) ? "Running" : "Stopped");
        }

        /// <summary>
        /// Disposes of the harness and stops the server
        /// </summary>
        public void Dispose()
        {
            this.DetachMonitor();
            this.Stop();
            if (this._server != null) this._server.Dispose();
        }
    }

    /// <summary>
    /// Server Harness for out of process servers
    /// </summary>
    class ExternalProcessServerHarness 
        : IServerHarness
    {
        private ProcessStartInfo _info;
        private Process _process;
        private String _host;
        private int _port;
        private ConsoleCapture _activeLogger;

        /// <summary>
        /// Creates a new out of process Server harness
        /// </summary>
        /// <param name="info">Process Start Info</param>
        public ExternalProcessServerHarness(ProcessStartInfo info)
        {
            this._info = info;
            Match m = Regex.Match(info.Arguments, @"-h(ost)? ([\w\-\.]+)");
            if (m.Success)
            {
                this._host = m.Groups[2].Value;
            }
            else
            {
                this._host = HttpServer.DefaultHost;
            }
            m = Regex.Match(info.Arguments, @"-p(ort)? (\d+)");
            if (m.Success)
            {
                this._port = Int32.Parse(m.Groups[2].Value);
            }
            else
            {
                this._port = RdfServerOptions.DefaultPort;
            }
        }

        /// <summary>
        /// Creates a new out of process Server harness
        /// </summary>
        /// <param name="info">Process Start Info</param>
        /// <param name="process">Process</param>
        public ExternalProcessServerHarness(ProcessStartInfo info, Process process)
            : this(info)
        {
            this._process = process;
        }

        #region IServerHarness Members

        /// <summary>
        /// Stops the Server
        /// </summary>
        public void Stop()
        {
            if (this._process != null)
            {
                if (this._activeLogger != null)
                {
                    this._activeLogger.Dispose();
                    this._activeLogger = null;
                }
                this._process.Kill();
                this._process = null;
            }
        }

        /// <summary>
        /// Starts the Server
        /// </summary>
        public void Start()
        {
            if (this._process == null)
            {
                //Ensure Start Info is correct
                this._info.UseShellExecute = false;
                this._info.RedirectStandardError = true;
                this._info.RedirectStandardOutput = true;
                this._info.CreateNoWindow = true;

                this._process = new Process();
                this._process.StartInfo = this._info;
                this._process.Start();
            }
        }

        /// <summary>
        /// Gets whether the Server is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return (this._process != null && !this._process.HasExited);
            }
        }

        /// <summary>
        /// Returns that pausing and resuming is not supported
        /// </summary>
        public bool CanPauseAndResume
        {
            get 
            {
                return false; 
            }
        }

        /// <summary>
        /// Throws an error as out of process servers cannot be paused
        /// </summary>
        public void Pause()
        {
            throw new NotSupportedException("Pause not supported by external process servers");
        }

        /// <summary>
        /// Throws an error as out of process servers cannot be resumed
        /// </summary>
        public void Resume()
        {
            throw new NotSupportedException("Resume not supported by external process servers");
        }

        /// <summary>
        /// Attaches a Monitor to the server
        /// </summary>
        /// <param name="monitor">Monitor</param>
        public void AttachMonitor(ServerMonitor monitor)
        {
            if (this._process != null)
            {
                this.DetachMonitor();
                if (this._activeLogger == null)
                {
                    this._activeLogger = new ConsoleCapture(this._process, monitor);
                }
            }
        }

        /// <summary>
        /// Detaches the current monitor from the server
        /// </summary>
        public void DetachMonitor()
        {
            if (this._activeLogger != null)
            {
                this._activeLogger.Dispose();
                this._activeLogger = null;
            }
        }

        #endregion

        /// <summary>
        /// Gets the string representation of the server
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[External Process] " + this._host + ":" + this._port + " - " + ((this.IsRunning) ? "Running" : "Stopped");
        }

        /// <summary>
        /// Disposes of the harness but leaves the external server running
        /// </summary>
        public void Dispose()
        {
            this.DetachMonitor();
            this._process = null;
        }
    }
}
