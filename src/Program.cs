using DotnetActionsToolkit;
using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SendWorkflowNotifications
{
    class Program
    {
        static void Main(string[] args)
        {
            var core = new Core();

            var smtpServer = core.GetInput("smtp-server");
            var smtpUser = core.GetInput("smtp-user");
            var smtpPassword = core.GetInput("smtp-password");
            var configFile = core.GetInput("configuration");

            configFile = @"C:\git\send-workflow-notifications\sample.yml";

            var input = File.ReadAllText(configFile);
            var deserializer = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();

            var config = deserializer.Deserialize<Configuration.NotificationsConfiguration>(input);
        }
    }
}
