using MongoDB.Bson;
using System;

namespace Tridion.Extensions.Reporting.Gamification
{
    public class TridionEvent
    {
        public ObjectId _id { get; set; }
        public string EventName { get; set; }
        public string EventPhase { get; set; }
        public DateTime EventStartDate { get; set; }
        public string SubjectId { get; set; }
        public string SubjectName { get; set; }
        public string SubjectType { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ServerName { get; set; }
        public long Ticks { get; set; }
        public string UniqueId { get; set; }
        public int EventThreadId { get; set; }
    }
}
