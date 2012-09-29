using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Gamification
{
    public class BadgeUtility
    {
        /// <summary>
        /// Gets all badges.
        /// </summary>
        /// <returns></returns>
        public static List<Badge> GetAllBadges()
        {
            List<Badge> badges = new List<Badge>();

            if (File.Exists("Badges.xml"))
            {
                XmlDocument badgesConfig = new XmlDocument();
                badgesConfig.Load("Badges.xml");
                foreach (XmlNode r in badgesConfig.SelectNodes("/Badges"))
                {
                    foreach (XmlNode node in r.ChildNodes)
                    {
                        Badge badge = Badge.Deserialize(node.OuterXml);
                        badges.Add(badge);
                    }
                }
            }
            return badges;
        }

        /// <summary>
        /// Checks all badges.
        /// </summary>
        /// <param name="tridionevent">The tridionevent.</param>
        /// <param name="tridionUser">The tridion user.</param>
        public static void CheckAllBadges(TridionEvent tridionevent, TridionUser tridionUser)
        {
            List<Badge> badges = BadgeUtility.GetAllBadges().Where(r => r.EventPhase == tridionevent.EventPhase).ToList();
            CheckEqualsOperator(badges, tridionUser);
        }

        /// <summary>
        /// Checks the equals operator.
        /// </summary>
        /// <param name="badges">The badges.</param>
        /// <param name="tridionUser">The tridion user.</param>
        private static void CheckEqualsOperator(List<Badge> badges, TridionUser tridionUser)
        {
            foreach (Badge badge in badges.Where(r=> r.Operator == Constants.Operators.Equals))
            {
                if (badge.MaxValue == tridionUser.EventCount)
                {
                    //we have a match, give this person a badge
                }
            }
        }
    }
}
