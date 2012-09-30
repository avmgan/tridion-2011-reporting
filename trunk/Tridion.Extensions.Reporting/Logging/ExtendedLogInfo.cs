using System;
using System.Threading;

using Tridion.ContentManager;
using Tridion.ContentManager.Extensibility;

namespace Tridion.Extensions.Reporting.Logging
{
    public class ExtendedLogInfo : BaseLogInfo
    {
        public ExtendedLogInfo() { }

        public ExtendedLogInfo(long startTime, IdentifiableObject subject, TcmEventArgs eventArgs, EventPhases phase)
        {
            Log(startTime, subject, eventArgs, phase);
        }

        public new void Log(long startTime, IdentifiableObject subject, TcmEventArgs eventArgs, EventPhases phase)
        {
            base.Log(startTime, subject, eventArgs, phase);

            Type baseType = eventArgs.GetType().BaseType;
            if (baseType != null) EventFamily = baseType.Name.Replace("EventArgs", "");

            EventPhase = phase.ToString();
            EventStartDate = DateTime.Now;
            SubjectType = subject.GetType().Name;
            UserId = subject.Session.User.Id.ToString();
            ServerName = Environment.MachineName;
            EventThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public string EventFamily { get; private set; }
        public string EventPhase { get; private set; }
        public DateTime EventStartDate { get; private set; }
        public string SubjectType { get; private set; }
        public string UserId { get; private set; }
        public string ServerName { get; private set; }
        public int EventThreadId { get; private set; }
    }
}
