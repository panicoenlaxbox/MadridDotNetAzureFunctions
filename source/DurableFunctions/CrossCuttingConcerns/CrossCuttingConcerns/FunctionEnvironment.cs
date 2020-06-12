using System;

namespace CrossCuttingConcerns
{
    public class FunctionEnvironment
    {
        public string WebSiteHostName { get; set; }
        public string EnvironmentName { get; set; }
        public string AppDirectory { get; set; }

        public bool HasEnvironment()
        {
            return !string.IsNullOrWhiteSpace(EnvironmentName);
        }

        public void EnsureHasEnvironment()
        {
            if (!HasEnvironment())
            {
                throw new Exception("Can't detect runtime environment");
            }
        }

        public bool IsEnvironment(string environment)
        {
            return EnvironmentName == environment;
        }

        public bool IsDevelopment()
        {
            return IsEnvironment("Development");
        }

        public void IfDevelopment(Action callback)
        {
            if (IsDevelopment())
            {
                callback();
            }
        }

        public bool IsStaging()
        {
            return IsEnvironment("Staging");
        }

        public void IfStaging(Action callback)
        {
            if (IsStaging())
            {
                callback();
            }
        }

        public bool IsProduction()
        {
            return IsEnvironment("Production");
        }

        public void IfProduction(Action callback)
        {
            if (IsProduction())
            {
                callback();
            }
        }
    }
}