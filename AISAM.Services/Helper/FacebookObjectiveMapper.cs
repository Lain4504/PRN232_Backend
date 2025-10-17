namespace AISAM.Services.Helper
{
    public static class FacebookObjectiveMapper
    {
        private static readonly Dictionary<string, string> ObjectiveMap = new()
        {
            { "VIDEO_VIEWS", "OUTCOME_TRAFFIC" },
            { "TRAFFIC", "OUTCOME_TRAFFIC" },
            { "CONVERSIONS", "OUTCOME_SALES" },
            { "LEAD_GENERATION", "OUTCOME_LEADS" },
            { "BRAND_AWARENESS", "OUTCOME_AWARENESS" },
            { "REACH", "OUTCOME_AWARENESS" },
            { "ENGAGEMENT", "OUTCOME_ENGAGEMENT" },
            { "APP_INSTALLS", "OUTCOME_APP_PROMOTION" },
            { "CATALOG_SALES", "OUTCOME_SALES" },
            { "STORE_TRAFFIC", "OUTCOME_TRAFFIC" },
            { "MESSAGES", "OUTCOME_MESSAGES" },
            { "EVENT_RESPONSES", "OUTCOME_EVENT_RESPONSES" }
        };

        /// <summary>
        /// Maps user-friendly objective names to Facebook API objective values
        /// </summary>
        /// <param name="userObjective">User-provided objective name</param>
        /// <returns>Facebook API objective value</returns>
        public static string MapToFacebookObjective(string userObjective)
        {
            if (string.IsNullOrWhiteSpace(userObjective))
                return userObjective;

            var upper = userObjective.ToUpper();
            return ObjectiveMap.TryGetValue(upper, out var mapped) ? mapped : upper;
        }

        /// <summary>
        /// Gets all supported user-friendly objective names
        /// </summary>
        /// <returns>List of valid objective names</returns>
        public static List<string> GetSupportedObjectives()
        {
            return ObjectiveMap.Keys.ToList();
        }

        /// <summary>
        /// Checks if the provided objective is supported
        /// </summary>
        /// <param name="objective">Objective to check</param>
        /// <returns>True if objective is supported</returns>
        public static bool IsSupportedObjective(string objective)
        {
            if (string.IsNullOrWhiteSpace(objective))
                return false;

            return ObjectiveMap.ContainsKey(objective.ToUpper());
        }
    }
}
