using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using Tridion.ContentManager;
using Tridion.ContentManager.Extensibility;

namespace Tridion.Extensions.Reporting.EventSystem
{
    [TcmExtension("Track All Events")]
    public class TrackEvents : TcmExtension, IDisposable
    {
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private bool _configLoaded;
        private const string ConfigXmlPath = "\\config\\ReportingConfig.xml";
        private XmlDocument _config;
        private MongoServer _server;
        private MongoDatabase _database;
        private const string MongoConnectionString = "mongodb://localhost/?safe=true";

        public TrackEvents()
        {
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            _subscriptions.Add(EventSystem.Subscribe<IdentifiableObject, TcmEventArgs>(LogStart, EventPhases.Initiated));
            _subscriptions.Add(EventSystem.SubscribeAsync<IdentifiableObject, TcmEventArgs>(LogEvent, EventPhases.TransactionCommitted | EventPhases.TransactionAborted | EventPhases.TransactionInDoubt));

            if (File.Exists(ConfigurationSettings.GetTcmHomeDirectory() + ConfigXmlPath))
            {
                _config = new XmlDocument();
                _config.Load(ConfigurationSettings.GetTcmHomeDirectory() + ConfigXmlPath);
                _configLoaded = true;
                
                _server = MongoServer.Create(MongoConnectionString);
                _database = _server.GetDatabase("TridionEvents");
            }
        }

        private static void LogStart(IdentifiableObject subject, TcmEventArgs tcmEventArgs, EventPhases phase)
        {
            if (tcmEventArgs.ContextVariables.ContainsKey("InitiatedTicks")) return;
            tcmEventArgs.ContextVariables.Add("InitiatedTicks", DateTime.Now.Ticks);
        }

        private void LogEvent(IdentifiableObject subject, TcmEventArgs tcmEventArgs, EventPhases phase)
        {
            string eventName = tcmEventArgs.GetType().Name;
            if(!_configLoaded) return;
            string xpath = string.Format("/Configuration/Events/Event[@Type='{0}']", eventName);
            bool skipSystemUsers =
                Convert.ToBoolean(_config.SelectSingleNode("/Configuration/Events").Attributes["SkipSystemUsers"].Value);
            if (skipSystemUsers && subject.Session.User.IsPredefined) return;
            
            if(subject.GetType().Name == "User" && (eventName == "LoadEventArgs" || eventName == "LoadAppDataEventArgs"))
            {
                bool skipLoadingProfile =
                    Convert.ToBoolean(
                        _config.SelectSingleNode("/Configuration/Events").Attributes["SkipLoadingUserProfile"].Value);
                if(skipLoadingProfile) return;
            }
            if(_config.SelectSingleNode(xpath) != null)
                if (_config.SelectSingleNode(xpath).Attributes["Enabled"].Value.ToLower() == "false") return;

            long startTime = Convert.ToInt64(tcmEventArgs.ContextVariables["InitiatedTicks"]);
            var collection = _database.GetCollection<BsonDocument>("events");
            TimeSpan ellapsed = new TimeSpan(DateTime.Now.Ticks - startTime);
            BsonDocument tridionEvent = new BsonDocument
                                                {
                                                    {"_id", ObjectId.GenerateNewId()},
                                                    {"EventName", tcmEventArgs.GetType().Name.Replace("EventArgs", "")},
                                                    {"EventFamily", tcmEventArgs.GetType().BaseType.Name.Replace("EventArgs", "")},
                                                    {"EventPhase", phase.ToString()},
                                                    {"EventStartDate", DateTime.Now},
                                                    {"SubjectId", subject.Id.ToString()},
                                                    {"SubjectName", subject.Title},
                                                    {"SubjectType", subject.GetType().Name},
                                                    {"UserId", subject.Session.User.Id.ToString()},
                                                    {"UserName", subject.Session.User.Title},
                                                    {"ServerName", Environment.MachineName},
                                                    {"EllapsedMilliseconds", ellapsed.Milliseconds},
                                                    {"EventThreadId", Thread.CurrentThread.ManagedThreadId}
                                                };
            collection.Insert(tridionEvent);
            

        }

        public void Dispose()
        {
            foreach (EventSubscription eventSubscription in _subscriptions)
            {
                eventSubscription.Unsubscribe();
            }
            _server.Disconnect();
        }
    }

}