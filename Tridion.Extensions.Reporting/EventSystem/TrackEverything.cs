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

namespace Tridion.Extensions.Reporting.EventSystem
{
    [TcmExtension("TrackEverythingEvents")]
    public class TrackEverything : TcmExtension, IDisposable
    {
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private const string CONFIG_XML_PATH = "\\config\\ReportingConfig.xml";
        private readonly XmlDocument _config = new XmlDocument();
        private bool _configLoaded = false;
        private string _auditServiceUrl = string.Empty;
        private string _defaultLogInfoAssemblyLocation = string.Empty;
        private string _defaultLogInfoTypeName = string.Empty;
        private AuditClient _client;
        private BasicHttpBinding _binding;
        private EndpointAddress _endpoint;

        public TrackEverything()
        {
            RegisterConfiguredEvents();
        }

        private void RegisterConfiguredEvents()
        {
            _subscriptions.Add(EventSystem.Subscribe<IdentifiableObject, TcmEventArgs>(LogStart, EventPhases.Initiated));
            _subscriptions.Add(EventSystem.SubscribeAsync<IdentifiableObject, TcmEventArgs>(CollectEvent, EventPhases.TransactionCommitted | EventPhases.TransactionAborted | EventPhases.TransactionInDoubt));

            if (File.Exists(ConfigurationSettings.GetTcmHomeDirectory() + CONFIG_XML_PATH))
            {
                _config.Load(ConfigurationSettings.GetTcmHomeDirectory() + CONFIG_XML_PATH);
                _configLoaded = true;

                XmlNode auditServiceUrlNode = _config.SelectSingleNode("/Configuration/AuditServiceUrl");
                if (auditServiceUrlNode != null)
                {
                    _auditServiceUrl = auditServiceUrlNode.InnerText;
                }

                XmlNode defaultLogInfoNode = _config.SelectSingleNode("/Configuration/LogInfoObject/add[@default = 'true']");
                if (defaultLogInfoNode != null)
                {
                    if (defaultLogInfoNode.Attributes["assembly"] == null) return;
                    if (defaultLogInfoNode.Attributes["class"] == null) return;

                    _defaultLogInfoAssemblyLocation = defaultLogInfoNode.Attributes["assembly"].Value.ToLower();
                    _defaultLogInfoTypeName = defaultLogInfoNode.Attributes["class"].Value;
                }
            }

            if (_auditServiceUrl.Equals(string.Empty)) return;
            // Open client
            _binding = new BasicHttpBinding(BasicHttpSecurityMode.None);
            _endpoint = new EndpointAddress(_auditServiceUrl);
            _client = new AuditClient(_binding, _endpoint);
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
            string logInfoAssemblyLocation = string.Empty;
            string logInfoTypeName = string.Empty;

            if (!_configLoaded || _auditServiceUrl.Equals(string.Empty)) return;

            string xpath = string.Format("/Configuration/Events/Event[@Type='{0}']", eventName);

            bool skipSystemUsers =
                Convert.ToBoolean(
                    _config.SelectSingleNode("/Configuration/Events").Attributes["SkipSystemUsers"].Value);
            if (skipSystemUsers && subject.Session.User.IsPredefined) return;

            if (subject.GetType().Name == "User" && (eventName == "LoadEventArgs" || eventName == "LoadAppDataEventArgs"))
            {
                bool skipLoadingProfile =
                    Convert.ToBoolean(
                        _config.SelectSingleNode("/Configuration/Events").Attributes["SkipLoadingUserProfile"].Value);
                if (skipLoadingProfile) return;
            }
            // Only log configured events

            // Log by default if node doesn't exist
            //if (config.SelectSingleNode(xpath) == null) return;
            if (_config.SelectSingleNode(xpath) != null)
                if (_config.SelectSingleNode(xpath).Attributes["Enabled"].Value.ToLower() == "false") return;

            XmlNode logInfoObject =
                _config.SelectSingleNode("/Configuration/LogInfoObject/add[@subjectType = '" + subject.GetType().Name +
                                        "']");
            if (logInfoObject != null)
            {
                logInfoAssemblyLocation = logInfoObject.Attributes["assembly"].Value.ToLower();
                logInfoTypeName = logInfoObject.Attributes["class"].Value;
            } else
            {
                logInfoAssemblyLocation = _defaultLogInfoAssemblyLocation;
                logInfoTypeName = _defaultLogInfoTypeName;
            }

            long startTime = Convert.ToInt64(tcmEventArgs.ContextVariables["InitiatedTicks"]);

            var logInfo = new object();

            if (logInfoAssemblyLocation.Equals(string.Empty) || logInfoTypeName.Equals(string.Empty)) return;

            Assembly logInfoAssembly = Assembly.LoadFile(logInfoAssemblyLocation);
            Type logInfoType = logInfoAssembly.GetType(logInfoTypeName);
            logInfo = Activator.CreateInstance(logInfoType, new object[] { startTime, subject, tcmEventArgs, phase });

            StoreAuditData(logInfo);

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
            }

            if (_auditServiceUrl.Equals(string.Empty)) return;
            
            _binding = new BasicHttpBinding(BasicHttpSecurityMode.None);
            _endpoint = new EndpointAddress(_auditServiceUrl);
            _client = new AuditClient(_binding, _endpoint);
            //_client = new AuditClient();
            _client.Open();

            _client.WriteEvent(auditData);
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
