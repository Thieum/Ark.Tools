﻿using Ark.Tools.Nodatime.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DiagnosticAdapter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ark.Tools.ResourceWatcher.ApplicationInsights
{ 
    public class ResourceWatcherDiagnosticListener : ResourceWatcherDiagnosticListenerBase
    {
        protected readonly TelemetryClient Client;
        protected readonly TelemetryConfiguration Configuration;

        private const string _type = "ProcessStep";

        public ResourceWatcherDiagnosticListener(TelemetryConfiguration configuration)
        {
            this.Configuration = configuration;
            this.Client = new TelemetryClient(configuration);
            this.Client.InstrumentationKey = configuration.InstrumentationKey;
        }

        #region Event
        [DiagnosticName("Ark.Tools.ResourceWatcher.HostStartEvent")]
        public override void OnHostStartEvent()
        {
            var telemetry = new EventTelemetry
            {
                Name = "Ark.Tools.ResourceWatcher.HostStartEvent",
            };
            
            this.Client.TrackEvent(telemetry);
        }

        [DiagnosticName("Ark.Tools.ResourceWatcher.RunTookTooLong")]
        public override void RunTookTooLong(string tenant, Activity activity)
        {
            var telemetry = new EventTelemetry
            {
                Name = activity.OperationName,
            };

            // properly fill dependency telemetry operation context
            telemetry.Context.Operation.Id = activity.RootId;
            telemetry.Context.Operation.ParentId = activity.ParentId;
            telemetry.Timestamp = activity.StartTimeUtc;

            //Properties and metrics
            telemetry.Properties.Add("Tenant", tenant);
            telemetry.Metrics.Add("ElapsedSeconds", activity.Duration.TotalSeconds);
            telemetry.Metrics.Add("ElapsedMinutes", activity.Duration.Minutes);

            this.Client.TrackEvent(telemetry);
        }

        [DiagnosticName("Ark.Tools.ResourceWatcher.ProcessResourceTookTooLong")]
        public override void OnProcessResourceTookTooLong(string tenant, string resourceId, Activity activity)
        {
            var telemetry = new EventTelemetry
            {
                Name = activity.OperationName,   
            };

            // properly fill dependency telemetry operation context
            telemetry.Context.Operation.Id = activity.RootId;
            telemetry.Context.Operation.ParentId = activity.ParentId;
            telemetry.Timestamp = activity.StartTimeUtc;

            //Properties and metrics
            telemetry.Properties.Add("Tenant", tenant);
            telemetry.Properties.Add("ResourceId", resourceId);
            telemetry.Metrics.Add("ElapsedSeconds", activity.Duration.TotalSeconds);
            telemetry.Metrics.Add("ElapsedMinutes", activity.Duration.Minutes);

            this.Client.TrackEvent(telemetry);
        }
        #endregion

        #region Exception
        [DiagnosticName("Ark.Tools.ResourceWatcher.ThrowDuplicateResourceIdRetrived")]
        public override void OnDuplicateResourceIdRetrived(string tenant, Exception exception)
        {
			Activity currentActivity = Activity.Current;

			var telemetryException = new ExceptionTelemetry
            {
                Exception = exception,
                Message = exception.Message
            };

            telemetryException.Properties.Add("Tenant", tenant);

			//Telemetry operation context
			telemetryException.Context.Operation.Id = currentActivity?.RootId;
			telemetryException.Context.Operation.ParentId = currentActivity?.ParentId;

			this.Client.TrackException(telemetryException);
        }

        [DiagnosticName("Ark.Tools.ResourceWatcher.ReportRunConsecutiveFailureLimitReached")]
        public override void OnReportRunConsecutiveFailureLimitReached(string tenant, Exception exception)
        {
			Activity currentActivity = Activity.Current;

			var telemetryException = new ExceptionTelemetry
            {
                Exception = exception,
                Message = exception.Message
            };

            telemetryException.Properties.Add("Tenant", tenant);

			//Telemetry operation context
			telemetryException.Context.Operation.Id = currentActivity?.RootId;
			telemetryException.Context.Operation.ParentId = currentActivity?.ParentId;

			this.Client.TrackException(telemetryException);
        }

        [DiagnosticName("Ark.Tools.ResourceWatcher.ProcessResourceSaveFailed")]
        public override void OnProcessResourceSaveFailed(string resourceId, string tenant, Exception exception)
        {
			Activity currentActivity = Activity.Current;

			var telemetryException = new ExceptionTelemetry
            {
                Exception = exception,
                Message = exception.Message
            };

            telemetryException.Properties.Add("Tenant", tenant);

			//Telemetry operation context
			telemetryException.Context.Operation.Id = currentActivity?.RootId;
			telemetryException.Context.Operation.ParentId = currentActivity?.ParentId;

			this.Client.TrackException(telemetryException);
        }
        #endregion

        #region Run
        [DiagnosticName("Ark.Tools.ResourceWatcher.Run.Stop")]
        public override void OnRunStop(   int resourcesFound
                                        , int normal
                                        , int noNewData
                                        , int noAction
                                        , int error
                                        , int skipped
                                        , string tenant
                                        , Exception exception)
        {
            Activity currentActivity = Activity.Current;

            var telemetry = new RequestTelemetry
            {
                Id = currentActivity.Id,
                Duration = currentActivity.Duration,
                Name = currentActivity.OperationName,
                Success = exception == null ? true : false,
                Timestamp = currentActivity.StartTimeUtc,
            };

            //Telemetry operation context
            telemetry.Context.Operation.Id = currentActivity.RootId;
            telemetry.Context.Operation.ParentId = currentActivity.ParentId;

            //Properties and metrics
            telemetry.Properties.Add("Tenant", tenant);
            telemetry.Metrics.Add("ResourcesFound", resourcesFound);
            telemetry.Metrics.Add("Result_Normal", normal);
            telemetry.Metrics.Add("Result_NoNewData", noNewData);
            telemetry.Metrics.Add("Result_NoAction", noAction);
            telemetry.Metrics.Add("Result_Error", error);
            telemetry.Metrics.Add("Result_Skipped", skipped);

            //Exception
            if (exception != null)
            {
                var telemetryException = new ExceptionTelemetry
                {
                    Exception = exception,
                    Message = exception.Message
                };

				//Telemetry operation context
				telemetryException.Context.Operation.Id = currentActivity.RootId;
				telemetryException.Context.Operation.ParentId = currentActivity.ParentId;

				//Properties and metrics
				telemetryException.Properties.Add("Tenant", tenant);
                telemetryException.Metrics.Add("ResourcesFound", resourcesFound);
                telemetryException.Metrics.Add("Result_Normal", normal);
                telemetryException.Metrics.Add("Result_NoNewData", noNewData);
                telemetryException.Metrics.Add("Result_NoAction", noAction);
                telemetryException.Metrics.Add("Result_Error", error);
                telemetryException.Metrics.Add("Result_Skipped", skipped);

                this.Client.TrackException(telemetryException);
            }

            this.Client.TrackRequest(telemetry);
        }
        #endregion 

        #region GetResources
        [DiagnosticName("Ark.Tools.ResourceWatcher.GetResources.Stop")]
        public override void OnGetResourcesStop(int resourcesFound, TimeSpan elapsed, string tenant, Exception exception)
        {
            Activity currentActivity = Activity.Current;

            var telemetry = new DependencyTelemetry
            {
                Id = currentActivity.Id,
                Duration = currentActivity.Duration,
                Name = currentActivity.OperationName,
                Success = exception == null ? true : false,
                Timestamp = currentActivity.StartTimeUtc,
                Type = _type
            };

            //Telemetry operation context
            telemetry.Context.Operation.Id = currentActivity.RootId;
            telemetry.Context.Operation.ParentId = currentActivity.ParentId;

            //Properties and metrics
            telemetry.Properties.Add("Tenant", tenant);
            telemetry.Properties.Add("Elapsed", elapsed.ToString());
            telemetry.Metrics.Add("ResourcesFound", resourcesFound);

            //Exception
            if (exception != null)
            {
                var telemetryException = new ExceptionTelemetry
                {
                    Exception = exception,
                    Message = exception.Message
                };

                telemetryException.Properties.Add("Tenant", tenant);
                telemetryException.Metrics.Add("ResourcesFound", resourcesFound);

                this.Client.TrackException(telemetryException);
            }

            this.Client.TrackDependency(telemetry);
        }
        #endregion

        #region CheckState

        [DiagnosticName("Ark.Tools.ResourceWatcher.CheckState.Stop")]
        public override void OnCheckStateStop(    int resourcesNew
                                                , int resourcesUpdated
                                                , int resourcesRetried
                                                , int resourcesRetriedAfterBan
                                                , int resourcesBanned
                                                , int resourcesNothingToDo
                                                , string tenant
                                                , Exception exception)
        {
            Activity currentActivity = Activity.Current;

            var telemetry = new DependencyTelemetry
            {
                Id = currentActivity.Id,
                Duration = currentActivity.Duration,
                Name = currentActivity.OperationName,
                Success = exception == null ? true : false,
                Timestamp = currentActivity.StartTimeUtc,
                Type = _type
            };

            //Telemetry operation context
            telemetry.Context.Operation.Id = currentActivity.RootId;
            telemetry.Context.Operation.ParentId = currentActivity.ParentId;

            //Properties and metrics
            telemetry.Properties.Add("Tenant", tenant);
            telemetry.Metrics.Add("Resources_New", resourcesNew);
            telemetry.Metrics.Add("Resources_Updated", resourcesUpdated);
            telemetry.Metrics.Add("Resources_Retried", resourcesRetried);
            telemetry.Metrics.Add("Resources_RetriedAfterBan", resourcesRetriedAfterBan);
            telemetry.Metrics.Add("Resources_Banned", resourcesBanned);
            telemetry.Metrics.Add("Resources_NothingToDo", resourcesNothingToDo);

            //Exception
            if (exception != null)
            {
                var telemetryException = new ExceptionTelemetry
                {
                    Exception = exception,
                    Message = exception.Message
                };

                telemetryException.Properties.Add("Tenant", tenant);

				//Telemetry operation context
				telemetryException.Context.Operation.Id = currentActivity.RootId;
				telemetryException.Context.Operation.ParentId = currentActivity.ParentId;

				//Properties and metrics
				telemetryException.Properties.Add("Tenant", tenant);
				telemetryException.Metrics.Add("Resources_New", resourcesNew);
				telemetryException.Metrics.Add("Resources_Updated", resourcesUpdated);
				telemetryException.Metrics.Add("Resources_Retried", resourcesRetried);
				telemetryException.Metrics.Add("Resources_RetriedAfterBan", resourcesRetriedAfterBan);
				telemetryException.Metrics.Add("Resources_Banned", resourcesBanned);
				telemetryException.Metrics.Add("Resources_NothingToDo", resourcesNothingToDo);

				this.Client.TrackException(telemetryException);
            }

            this.Client.TrackDependency(telemetry);
        }
        #endregion

        #region ProcessEntry
        [DiagnosticName("Ark.Tools.ResourceWatcher.ProcessResource.Stop")]
        public override void OnProcessResourceStop(string tenant, ProcessContext processContext, Exception exception)
        {
            Activity currentActivity = Activity.Current;

            var telemetry = new DependencyTelemetry
            {
                Id = currentActivity.Id,
                Duration = currentActivity.Duration,
                Name = currentActivity.OperationName,
                Success = exception == null ? true : false,
                Timestamp = currentActivity.StartTimeUtc,
                Type = _type
            };

            //Telemetry operation context
            telemetry.Context.Operation.Id = currentActivity.RootId;
            telemetry.Context.Operation.ParentId = currentActivity.ParentId;

            //Properties and metrics
            telemetry.Properties.Add("Tenant", tenant);
            _propertiesProcessContext(telemetry, processContext);

            //Exception
            if (exception != null)
            {
                var telemetryException = new ExceptionTelemetry
                {
                    Exception = exception,
                    Message = exception.Message
                };

                //Telemetry operation context
                telemetryException.Context.Operation.Id = currentActivity.RootId;
                telemetryException.Context.Operation.ParentId = currentActivity.ParentId;

                //Properties and metrics
                telemetryException.Properties.Add("Tenant", tenant);
                _propertiesProcessContext(telemetryException, processContext);

                this.Client.TrackException(telemetryException);
            }

            this.Client.TrackDependency(telemetry);
        }

        #endregion

        #region FetchResource
        [DiagnosticName("Ark.Tools.ResourceWatcher.FetchResource.Stop")]
        public override void OnFetchResourceStop(string tenant, ProcessContext processContext, Exception exception)
        {
            Activity currentActivity = Activity.Current;

            var telemetry = new DependencyTelemetry
            {
                Id = currentActivity.Id,
                Duration = currentActivity.Duration,
                Name = currentActivity.OperationName,
                Success = exception == null ? true : false,
                Timestamp = currentActivity.StartTimeUtc,
                Type = _type
            };

            //Telemetry operation context
            telemetry.Context.Operation.Id = currentActivity.RootId;
            telemetry.Context.Operation.ParentId = currentActivity.ParentId;

            //Properties and metrics
            telemetry.Properties.Add("Tenant", tenant);
            _propertiesProcessContext(telemetry, processContext);

            //Exception
            if (exception != null)
            {
                var telemetryException = new ExceptionTelemetry
                {
                    Exception = exception,
                    Message = exception.Message
                };

                //Telemetry operation context
                telemetryException.Context.Operation.Id = currentActivity.RootId;
                telemetryException.Context.Operation.ParentId = currentActivity.ParentId;

                //Properties and metrics
                telemetryException.Properties.Add("Tenant", tenant);
                _propertiesProcessContext(telemetryException, processContext);

                this.Client.TrackException(telemetryException);
            }

            this.Client.TrackDependency(telemetry);
        }
        #endregion



        private void _propertiesProcessContext(ISupportProperties data, ProcessContext pc)
        {
            data.Properties.Add("ResourceId", pc.CurrentInfo.ResourceId);
            data.Properties.Add("ProcessType", pc.ProcessType.ToString());
            data.Properties.Add("ResultType", pc.ResultType.ToString());

            data.Properties.Add("Idx/Total", pc.Index.ToString() + "/" + pc.Total.ToString());

            if (pc.LastState != default)
            {
                data.Properties.Add("CheckSum_Old", pc.LastState.CheckSum);
                data.Properties.Add("Modified_Old", pc.LastState.Modified.ToString());
            }

            if (pc.NewState != default)
            {
                data.Properties.Add("RetryCount", pc.NewState.RetryCount.ToString());
                data.Properties.Add("RetrievedAt", pc.NewState.ToString());
                data.Properties.Add("CheckSum", pc.NewState.CheckSum);
                data.Properties.Add("Modified", pc.NewState.Modified.ToString());

                string extensionsString = JsonConvert.SerializeObject(pc.NewState.Extensions, ArkDefaultJsonSerializerSettings.Instance);
                data.Properties.Add("Extensions", extensionsString);
            }
        }
    }
}
