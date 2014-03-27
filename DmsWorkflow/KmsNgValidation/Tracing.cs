using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Online.Management.WorkflowContracts.Extensions;

namespace KmsNgWorkflow
{
    // First modification to test best common-ancestor merge base in Testing branch
    public interface ITrace
    {
        void LogMessage(TraceLevel traceLevel, string format, params object[] args);
    }

    public class Tracing : ITrace
    {
        public Tracing(IStreamExtension streamExtension, string targetName, string workflowName)
        {
            this._streamExtension = streamExtension;
            this._targetName = targetName;
            this._workflowName = workflowName;
        }

        public static ITrace Instance { get; set; }

        public void LogMessage(TraceLevel traceLevel, string format, params object[] args)
        {
            string inputMessage = string.Format(format, args);
            string logMessage = string.Format("{0}: {1}", traceLevel, inputMessage);

            this._streamExtension.SendProgress(_workflowName, this._targetName, logMessage, DateTime.UtcNow);
        }

        private readonly IStreamExtension _streamExtension;
        private readonly string _targetName;
        private readonly string _workflowName;
    }
}
