using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Gamification
{
    public class ChallengeUtility
    {
        /// <summary>
        /// Gets all challenges.
        /// </summary>
        /// <returns></returns>
        public static List<Challenge> GetAllChallenges()
        {
            List<Challenge> challenges = new List<Challenge>();

            if (File.Exists("Configuration\\Challenges.xml"))
            {
                XmlDocument challengesConfig = new XmlDocument();
                challengesConfig.Load("Configuration\\Challenges.xml");
                foreach (XmlNode c in challengesConfig.SelectNodes("/Challenges"))
                {
                    foreach (XmlNode node in c.ChildNodes)
                    {
                        Challenge challenge = Challenge.Deserialize(c.OuterXml);
                        challenges.Add(challenge);
                    }
                }
            }
            return challenges;
        }

        /// <summary>
        /// Checks all Challenges.
        /// </summary>
        public static void CheckAllChallenges(TridionUser tridionUser)
        {
            //get the badges completed by this user
            string[] badgesCompleted = tridionUser.BadgesCompleted.Split(',');
            foreach (Challenge c in GetAllChallenges())
            {
                //check if this challenge has already been completed
                if (!tridionUser.ChallengesCompleted.Contains(c.Id))
                {
                    //get the badges that need to be completed for this challenge
                    string[] badgesToComlplete = c.BadgesToComplete.Split(',');

                    bool completedAll = false;
                    foreach (string badge in badgesToComlplete)
                    {
                        if (badgesCompleted.Contains(badge))
                        {
                            completedAll = true;
                        }
                        else
                        {
                            completedAll = false;
                            break;
                        }
                    }

                    if (completedAll)
                    {
                        //mark this challenge as completed and give the user a badge!
                    }
                }
            }
        }
    }
}
