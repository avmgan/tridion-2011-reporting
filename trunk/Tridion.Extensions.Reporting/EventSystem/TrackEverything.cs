using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Xml;

using Tridion.ContentManager;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Extensibility;
using Tridion.ContentManager.Extensibility.Events;

using Tridion.Extensions.Reporting.EventSystem;

namespace Tridion.Extensions.Reporting.EventSystem
{
    [TcmExtension("TrackEverythingEvents")]
    public class TrackEverything : TcmExtension, IDisposable
    {

        private static string _mongoConnectionString = "mongodb://localhost/?safe=true";
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private AuditClient _client;

        public TrackEverything()
        {
            RegisterConfiguredEvents();
        }

        private void RegisterConfiguredEvents()
        {
            _subscriptions.Add(EventSystem.Subscribe<IdentifiableObject, TcmEventArgs>(LogStart, EventPhases.Initiated));
            _subscriptions.Add(EventSystem.SubscribeAsync<IdentifiableObject, TcmEventArgs>(CollectEvent, EventPhases.TransactionCommitted | EventPhases.TransactionAborted | EventPhases.TransactionInDoubt));

            _client = new AuditClient();
        }


        private void SetDefaultValues(OrganizationalItem subject, LoadEventArgs args, EventPhases phase)
        {
            if (subject.Id.IsUriNull) //Is new
            {
                OrganizationalItem parent = subject.OrganizationalItem;
                if (parent.MetadataSchema != null && subject.MetadataSchema == null)
                {
                    subject.MetadataSchema = parent.MetadataSchema;
                    subject.Metadata = parent.Metadata;
                }
            }
        }

        private void LogStart(IdentifiableObject subject, TcmEventArgs tcmEventArgs, EventPhases phase)
        {
            if (tcmEventArgs.ContextVariables.ContainsKey("InitiatedTicks")) return;
            tcmEventArgs.ContextVariables.Add("InitiatedTicks", DateTime.Now.Ticks);
        }

        private void CollectEvent(IdentifiableObject subject, TcmEventArgs tcmEventArgs, EventPhases phase)
        {

            string eventName = tcmEventArgs.GetType().Name;
            string assemblyLocation = string.Empty;
            string logInfoObjectClass = string.Empty;

            if (File.Exists(ConfigurationSettings.GetTcmHomeDirectory() + "\\config\\ReportingConfig.xml"))
            {
                XmlDocument config = new XmlDocument();
                config.Load(ConfigurationSettings.GetTcmHomeDirectory() + "\\config\\ReportingConfig.xml");
                string xpath = string.Format("/Configuration/Events/Event[@Type='{0}']", eventName);

                bool skipSystemUsers =
                    Convert.ToBoolean(
                        config.SelectSingleNode("/Configuration/Events").Attributes["SkipSystemUsers"].Value);
                if (skipSystemUsers && subject.Session.User.IsPredefined) return;

                if (subject.GetType().Name == "User" && (eventName == "LoadEventArgs" || eventName == "LoadAppDataEventArgs"))
                {
                    bool skipLoadingProfile =
                        Convert.ToBoolean(
                            config.SelectSingleNode("/Configuration/Events").Attributes["SkipLoadingUserProfile"].Value);
                    if (skipLoadingProfile) return;
                }
                // Only log configured events

                // Log by default if node doesn't exist
                //if (config.SelectSingleNode(xpath) == null) return;
                if (config.SelectSingleNode(xpath) != null)
                    if (config.SelectSingleNode(xpath).Attributes["Enabled"].Value.ToLower() == "false") return;
                _mongoConnectionString = config.SelectSingleNode("/Configuration/MongoDbUrl").InnerText;

                XmlNode logInfoObject =
                    config.SelectSingleNode("/Configuration/LogInfoObject/add[@subjectType = '" + subject.GetType().Name +
                                            "']");
                if (logInfoObject != null)
                {
                    assemblyLocation = logInfoObject.Attributes["assembly"].Value.ToLower();
                    logInfoObjectClass = logInfoObject.Attributes["class"].Value.ToLower();
                }

            }

            long startTime = Convert.ToInt64(tcmEventArgs.ContextVariables["InitiatedTicks"]);

            var logInfo = new object();

            if (assemblyLocation != string.Empty && logInfoObjectClass != string.Empty)
            {
                Assembly logInfoAssembly = Assembly.LoadFile(assemblyLocation);
                Type logInfoType = logInfoAssembly.GetType("Logging.ExtendedLogInfo");
                logInfo = Activator.CreateInstance(logInfoType,
                                                    new object[] { startTime, subject, tcmEventArgs, phase });

                StoreAuditData(logInfo);
            }
        }

        private void StoreAuditData(object auditData)
        {
            if (_client.State == CommunicationState.Opened)
                _client.WriteEvent(auditData);
            if (_client.State != CommunicationState.Opened)
            {
                if (_client.State != CommunicationState.Faulted)
                    _client.Close();
                else
                    _client.Abort();

                //_client = new AuditClient(_binding, _endpoint);
                _client = new AuditClient();
                _client.Open();
            }
        }

        public void Dispose()
        {
            foreach (EventSubscription eventSubscription in _subscriptions)
            {
                eventSubscription.Unsubscribe();
            }
        }
    }
}
