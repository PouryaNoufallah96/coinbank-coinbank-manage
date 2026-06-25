namespace Utilities.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CustomRateLimitAttribute(string message = "Too many requests. Try again %%time min later",
        int maxAttemptsCount = 55, int periodSeconds = 60, int lockoutDurationMinutes = 5,
        string requestBodyKey = null) : Attribute
    {
        public string Message { get; } = message.Replace("%%time", lockoutDurationMinutes.ToString());
        public int MaxAttemptsCount { get; } = maxAttemptsCount;
        public int PeriodSeconds { get; set; } = periodSeconds;
        public int LockoutDurationMinutes { get; } = lockoutDurationMinutes;
        public string RequestBodyKey { get; } = requestBodyKey;
    }
}
