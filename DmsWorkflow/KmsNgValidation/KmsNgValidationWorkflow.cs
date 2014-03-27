using System;
using System.Activities;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Online.Management.Entities;
using Microsoft.Online.Management.WorkflowContracts.ActivityBase;
using Microsoft.Online.Management.WorkflowContracts.Extensions;

namespace KmsNgWorkflow
{
    public sealed class KmsNgValidationWorkflow : AsyncActionActivity
    {
        #region Interface

        /// <summary>
        /// Gets or sets the Topology Id
        /// </summary>
        [RequiredArgument]
        public InArgument<int> TopologyId { get; set; }

        /// <summary>
        /// Gets or sets the RoleSlice Id
        /// </summary>
        [RequiredArgument]
        public InArgument<int> RoleSliceId { get; set; }

        /// <summary>
        /// Gets or sets the Azure deployment Id
        /// </summary>
        [RequiredArgument]
        public InArgument<string> AzureDeploymentId { get; set; }
        
        protected override Action CreateImplementation(
            AsyncCodeActivityContext context,
            CancellationToken cancellationToken)
        {
            this._topologyExtension = ExtensionHelper.GetExtension<ITopologyExtension>(context);

            this._topologyId = this.TopologyId.Get(context);
            this._roleSliceId = this.RoleSliceId.Get(context);
            this._deploymentId = this.AzureDeploymentId.Get(context);
            
            Tracing.Instance = new Tracing(
                ExtensionHelper.GetExtension<IStreamExtension>(context),
                this._roleSliceId.ToString(CultureInfo.InvariantCulture),
                WorkflowName);

            LogMessage(TraceLevel.Info, "{0} called", WorkflowName);
            return () => this.TaskImplementation();
        }

        #endregion

        #region Helper Methods

        private void TaskImplementation()
        {
            try
            {
                this.ValidateTopology();

                ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => { return true; });

                this.RunDvt();
                LogMessage(TraceLevel.Info, "{0} succeeded.", WorkflowName);
            }
            catch (Exception e)
            {
                if (IsFatal(e))
                {
                    throw;
                }

                LogMessage(TraceLevel.Error, "{0} failed. Exception Info: {1}", WorkflowName, e.ToString());
                throw;
            }
        }

        private void ValidateTopology()
        {
            LogMessage(TraceLevel.Info, "{0}: {1}", WorkflowName, "ValidateTopology");

            string message;

            Topology topology = this._topologyExtension.GetTopology(this._topologyId);
            if (topology == null || 
                topology.Roles == null ||
                topology.RoleSlices == null)
            {
                message = "Topology or Roles Or RoleSlices properties for TopologyId '{0}' " +
                    "not available from DMS as topology extension returned null";
                FailValidation(message, this._topologyId);
            }

            RoleSlice roleSlice = topology.RoleSlices.SingleOrDefault(rs => rs.Id == this._roleSliceId);
                
            if (roleSlice == null)
            {
                message = "RoleSlice properties for TopologyId '{0}', RoleSliceId '{1}'  " +
                    "not available from DMS as topology extension returned null";   
                FailValidation(message, this._topologyId, this._roleSliceId);
            }

            Role targetRole = topology.Roles.SingleOrDefault(role => role.Id == roleSlice.RoleId);
            if (targetRole == null)
            {
                message = "Role properties for TopologyId '{0}', RoleId '{1}' " +
                    "not available from DMS as topology extension returned null";
                FailValidation(message, this._topologyId, roleSlice.RoleId);
            }

            DeploymentUnit targetDu = topology.DeploymentUnits.SingleOrDefault(du => du.Id == targetRole.DeploymentUnitId);
            if (targetDu == null)
            {
                message = "DeploymentUnit property for TopologyID '{0}', RoleId '{1}', DeploymentUnitId '{2}' " +
                    "not available from DMS as topology extension returned null";
                FailValidation(message, this._topologyId, targetRole.Id, targetRole.DeploymentUnitId);
            }

            message = "Deployment Id: {0}, Topology Name(Id): {1}({2}), Deployment Unit(Id): {3}({4}), " +
                      "Role Name(Id): {5}({6}), RoleSlice(Id): {7}({8})";
            LogMessage(TraceLevel.Info, 
                message,
                this._deploymentId,
                topology.Name,
                topology.Id,
                targetDu.Name,
                targetDu.Id,
                targetRole.Name,
                targetRole.Id,
                roleSlice.Name,
                roleSlice.Id);
        }

        private void RunDvt()
        {
            string baseAddress = string.Format("https://{0}.{1}", this._deploymentId, Configuration.AzureDomainName);
            LogMessage(TraceLevel.Info, "{0}: RunDVT against {1}", WorkflowName, baseAddress);

            (new DVTRunner(baseAddress, WorkflowName)).Run();
            LogMessage(TraceLevel.Info, "RunDVT succeeded");
        }
        
        private static bool IsFatal(Exception exception)
        {
            bool returnValue =
                (exception is OutOfMemoryException && !(exception is InsufficientMemoryException)) ||
                exception is AccessViolationException ||
                exception is System.Runtime.InteropServices.SEHException ||
                exception is TypeInitializationException ||
                exception is TargetInvocationException;

            return returnValue;
        }

        private void LogMessage(TraceLevel traceLevel, string format, params object[] args)
        {
            Tracing.Instance.LogMessage(traceLevel, format, args);
        }

        private void FailValidation(string format, params object[] args)
        {
            Helper.FailValidation(format, args);
        }

        #endregion

        #region Private Members

        public const string WorkflowName = "KmsNgValidationWorkflow";

        private ITopologyExtension _topologyExtension;
        private int _topologyId;
        private int _roleSliceId;
        private string _deploymentId;
       
       
        #endregion
    }
}
