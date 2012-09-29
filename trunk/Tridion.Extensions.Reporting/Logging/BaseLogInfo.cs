using System;

using Tridion.ContentManager;
using Tridion.ContentManager.Extensibility;

namespace Tridion.Extensions.Reporting.Logging
{
    public class BaseLogInfo
    {
        public BaseLogInfo() { }

        public BaseLogInfo(long startTime, IdentifiableObject subject, TcmEventArgs eventArgs, EventPhases phase)
        {
            Log(startTime, subject, eventArgs, phase);
        }

        public void Log(long startTime, IdentifiableObject subject, TcmEventArgs eventArgs, EventPhases phase)
        {
            Uri = subject.Id.ToString();
            Title = subject.Title;
            EventName = eventArgs.GetType().Name.Replace("EventArgs", "");
            UserName = subject.Session.User.Title;

            long ticks = DateTime.Now.Ticks;
            TimeSpan elapsed = new TimeSpan(ticks - startTime);
            ElapsedMilliseconds = elapsed.Milliseconds;
        }

        public string Uri { get; private set; }

        public string Title { get; private set; }

        public string EventName { get; private set; }

        public string UserName { get; private set; }

        public int ElapsedMilliseconds { get; private set; }
    }
}
