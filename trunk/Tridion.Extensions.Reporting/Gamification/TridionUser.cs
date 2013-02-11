using MongoDB.Bson;

namespace Tridion.Extensions.Reporting.Gamification
{
    public class TridionUser
    {
        public ObjectId _id { get; set; }
        public string UserName { get; set; }
        public int EventCount { get { return 10; } }
        public string BadgesCompleted { get { return "1,2"; } }
        public string ChallengesCompleted { get { return ""; } }
    }
}
