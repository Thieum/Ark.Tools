﻿using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Ark.Tools.ResourceWatcher
{
    internal class ResourceWatcherDiagnosticSource
    {
        public const string DiagnosticListenerName = "Ark.Tools.ResourceWatcher";
        public const string BaseActivityName = "Ark.Tools.ResourceWatcher";
        public const string ExceptionEventName = BaseActivityName + "Exception";

        private readonly string _tenant;
        private static Logger _logger;

        private static readonly DiagnosticListener _source = new DiagnosticListener(DiagnosticListenerName);

        public ResourceWatcherDiagnosticSource(string tenant, Logger logger)
        {
            _tenant = tenant;
            _logger = logger;
        }

        #region Event
        public void HostStartEvent()
        {
            _reportEvent("HostStartEvent", () => new { });
        }

        public void RunTookTooLong(TimeSpan elapsed)
        {
            _logger.Fatal($"Check for tenant {_tenant} took too much:{elapsed}");

            _reportEvent("RunTookTooLong",
                () => new
                {
                    Elapsed = elapsed,
                    Tenant = _tenant,
                });
        }

        public void ProcessResourceTookTooLong(string resourceId, TimeSpan elapsed)
        {
            _logger.Fatal($"Processing of ResourceId=\"{resourceId}\" took too much: {elapsed}");

            _reportEvent("ProcessResourceTookTooLong",
                () => new
                {
                    ResourceId = resourceId,
                    Elapsed = elapsed,
                    Tenant = _tenant,
                });
        }
        #endregion

        #region Run
        public Activity RunStart(RunType type, DateTime now)
        {
            _logger.Info($"Check started for tenant {_tenant} at {now}");

            Activity activity = _start("Run", () => new
            {
                Type = type,
                Now = now,
                Tenant = _tenant,
            },
                null
            );

            return activity;
        }

        public void RunFailed(Activity activity, Exception ex, TimeSpan elapsed)
        {
            _logger.Error(ex, $"Check failed for tenant {_tenant} in {elapsed}");

            _stop(activity, () => new
            {
                Exception = ex,
                Elapsed = elapsed,
                Tenant = _tenant,
            }
            );
        }

        public void RunSuccessful(Activity activity, List<ProcessContext> evaluated, TimeSpan elapsed)
        {
            _logger.Info($"Check successful for tenant {_tenant} in {elapsed}");

            _stop(activity, () =>
            {
                var total = 0;
                var counts = evaluated.GroupBy(x => x.ResultType).ToDictionary(x => x.Key, x => x.Count());
                foreach (var k in Enum.GetValues(typeof(ResultType)).Cast<ResultType>())
                {
                    if (!counts.ContainsKey(k))
                        counts[k] = 0;

                    total += counts[k];
                }

                return new
                {
                    ResourcesFound = total,
                    Normal = counts[ResultType.Normal],
                    NoNewData = counts[ResultType.NoNewData],
                    NoAction = counts[ResultType.NoAction],
                    Error = counts[ResultType.Error],
                    Skipped = counts[ResultType.Skipped],
                    Tenant = _tenant,
                };
            }
            );
        }
        #endregion

        #region GetResources
        public Activity GetResourcesStart()
        {
            Activity activity = _start("GetResources", () => new
            {
            },
                null
            );

            return activity;
        }

        public void GetResourcesFailed(Activity activity, Exception ex)
        {
            _stop(activity, () => new
            {
                Exception = ex,
                Tenant = _tenant,
            }
            );
        }

        public void GetResourcesSuccessful(Activity activity, int count, TimeSpan elapsed)
        {
            _logger.Info($"Found {count} resources in {elapsed}");

            _stop(activity, () => new
            {
                ResourcesFound = count,
                Elapsed = elapsed,
                Tenant = _tenant,
            }
            );
        }
        #endregion

        #region CheckState
        public Activity CheckStateStart()
        {
            Activity activity = _start("CheckState", () => new
            {
            },
                null
            );

            return activity;
        }

        public void CheckStateSuccessful(Activity activity, IEnumerable<ProcessContext> evaluated)
        {
            _stop(activity, () =>
            {
                var counts = evaluated.GroupBy(x => x.ProcessType).ToDictionary(x => x.Key, x => x.Count());
                foreach (var k in Enum.GetValues(typeof(ProcessType)).Cast<ProcessType>())
                    if (!counts.ContainsKey(k))
                        counts[k] = 0;

                return new
                {
                    ResourcesNew = counts[ProcessType.New],
                    ResourcesUpdated = counts[ProcessType.Updated],
                    ResourcesRetried = counts[ProcessType.Retry],
                    ResourcesRetriedAfterBan = counts[ProcessType.RetryAfterBan],
                    ResourcesBanned = counts[ProcessType.Banned],
                    ResourcesNothingToDo = counts[ProcessType.NothingToDo],
                    Tenant = _tenant,
                };
            }
            );
        }
        #endregion

        #region ProcessResource
        public Activity ProcessResourceStart(int idx, int total, ProcessContext processContext)
        {
            _logger.Info("({4}/{5}) Detected change on ResourceId=\"{0}\", Resource.Modified={1}, OldState.Modified={2}, OldState.Retry={3}. Processing..."
                , processContext.CurrentInfo.ResourceId
                , processContext.CurrentInfo.Modified
                , processContext.LastState?.Modified
                , processContext.LastState?.RetryCount
                , idx
                , total
            );

            Activity activity = _start("ProcessResource", () => new
            {
                Idx = idx,
                Total = total,
                ProcessContext = processContext,
                Tenant = _tenant,
            },
                null
            );

            return activity;
        }

        public void ProcessResourceFailed(Activity activity, int idx, int total, ProcessContext processContext, bool isBanned, Exception ex)
        {
            var lvl = isBanned ? LogLevel.Fatal : LogLevel.Warn;
            _logger.Log(lvl, ex, $"({idx}/{total}) ResourceId=\"{processContext.CurrentInfo.ResourceId}\" process Failed");

            _stop(activity, () => new
            {
                Idx = idx,
                Total = total,
                ProcessContext = processContext,
                IsBanned = isBanned,
                Exception = ex,
                Tenant = _tenant,
            }
            );
        }

        public void ProcessResourceSuccessful(Activity activity, int idx, int total, ProcessContext processContext)
        {
            //_setTags(activity, processType.ToString(), processType.ToString());

            _stop(activity, () => new
            {
                Idx = idx,
                Total = total,
                ProcessContext = processContext,
                Tenant = _tenant,
            }
            );

            if (processContext.ResultType == ResultType.NoNewData)
            {
                _logger.Info($"({idx}/{total}) ResourceId=\"{processContext.CurrentInfo.ResourceId}\" No payload retrived, so no new state. Generally due to a same-checksum");
            }
            else if (processContext.ResultType == ResultType.NoAction)
            {
                _logger.Info($"({idx}/{total}) ResourceId=\"{processContext.CurrentInfo.ResourceId}\" No action has been triggered and payload has not been retrieved. We do not change the state");
            }
            else if (processContext.ResultType == ResultType.Normal)
            {
                _logger.Info($"({idx}/{total}) ResourceId=\"{processContext.CurrentInfo.ResourceId}\" handled {(processContext.NewState.RetryCount == 0 ? "" : "not ")}successfully in {activity.Duration}");
            }
        }
        #endregion

        #region Exception
        public void ProcessResourceSaveFailed(string resourceId, Exception ex)
        {
            _logger.Error(ex, $"Saving of ResourceId=\"{resourceId}\" failed");

            _reportException("ProcessResourceSaveFailed", ex);
        }

        public void ThrowDuplicateResourceIdRetrived(string duplicateId)
        {
            var ex = new InvalidOperationException($"Found multiple entries for ResouceId: {duplicateId}");

            _reportException("ThrowDuplicateResourceIdRetrived", ex);

            throw ex;            
        }

        public void ReportRunConsecutiveFailureLimitReached(Exception ex, int count)
        {
            _logger.Fatal($"Failed {count} times consecutively");

            _reportException("ReportRunConsecutiveFailureLimitReached", ex);
        }
        #endregion

        private Activity _start(string operationName, Func<object> getPayload, Action<Activity> setTags)
        {
            Activity activity = null;
            string activityName = BaseActivityName + "." +operationName;

            if (_source.IsEnabled(activityName))
            {
                activity = new Activity(activityName);
                setTags?.Invoke(activity);

                if (_source.IsEnabled(activityName + ".Start"))
                {
                    _source.StartActivity(activity, getPayload());
                }
                else
                {
                    activity.Start();
                }
            }

            return activity;
        }

        internal void _stop(Activity activity, Func<object> getPayload)
        {
            if (activity != null)
            {
                _source.StopActivity(activity, getPayload());
            }
        }

        private void _setTags(Activity activity, string messageKey, string message)
        {
            if (activity != null)
            {
                if (message == null)
                {
                    return;
                }

                activity.AddTag(messageKey, message);
            }
        }

        internal void _reportEvent(string eventName, Func<object> getPayload)
        {
            var name = BaseActivityName + "." + eventName;

            if (_source.IsEnabled(name))
            {
                _source.Write(name, getPayload());
            }
        }

        internal void _reportException(string exceptionName, Exception ex)
        {
            var name = BaseActivityName + "." + exceptionName;

            if (_source.IsEnabled(name))
            {
                _source.Write(name,
                    new
                    {
                        Exception = ex,
                        Tenant = _tenant
                    });
            }
        }
    }
}