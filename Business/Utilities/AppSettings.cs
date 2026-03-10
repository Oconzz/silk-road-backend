using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Utilities
{
    public class AppSettings
    {
        // Existing/legacy fields still used in code
        public string Secret { get; set; }
        // Mirrored structure from APIServer/appsettings.json
        public LoggingSettings Logging { get; set; }
        public string AllowedHosts { get; set; }
        public ConnectionStringsSettings ConnectionStrings { get; set; }
        public MailGunSettings MailGun { get; set; }
        public AwsSettings AwsSettings { get; set; }
        public OneSignalSettings OneSignal { get; set; }
    }

    public class LoggingSettings
    {
        public LogLevelSettings LogLevel { get; set; }
    }

    public class LogLevelSettings
    {
        public string Default { get; set; }
        [ConfigurationKeyName("Microsoft.AspNetCore")]
        public string MicrosoftAspNetCore { get; set; }
    }

    public class ConnectionStringsSettings
    {
        public string LocalConnectionString { get; set; }
        public string StagingConnectionString { get; set; }
        public string ProductionConnectionString { get; set; }

        public string LocalConnectionStringdbUser { get; set; }
        public string LocalConnectionStringdbPass { get; set; }
        public string LocalConnectionStringdbName { get; set; }
        public string LocalConnectionStringdbServer { get; set; }

        public string StagingConnectionStringdbUser { get; set; }
        public string StagingConnectionStringdbPass { get; set; }
        public string StagingConnectionStringdbName { get; set; }
        public string StagingConnectionStringdbServer { get; set; }

        public string ProductionConnectionStringdbUser { get; set; }
        public string ProductionConnectionStringdbPass { get; set; }
        public string ProductionConnectionStringdbName { get; set; }
        public string ProductionConnectionStringdbServer { get; set; }
    }

    public class MailGunSettings
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
        public string DefaultSender { get; set; }
    }

    public class AwsSettings
    {
        public string AWS_ACCESS_KEY { get; set; }
        public string AWS_SECRET_KEY { get; set; }
        public string AWSAccessKeyKMS { get; set; }
        public string AWSSecretKeyKMS { get; set; }
        public string KMSKeyId { get; set; }
        public string S3PublicBucket { get; set; }
        public string S3PrivateBucket { get; set; }
        public string S3Region { get; set; } = "us-east-1";
    }


    public class OneSignalSettings
    {
        public string AppId { get; set; }
        public string ApiKey { get; set; }
        public string PerformanceCreateTitle { get; set; }
        public string PerformanceCreateContent { get; set; }
        public string PerformanceUpdateTitle { get; set; }
        public string PerformanceUpdateContent { get; set; }
    }
}
