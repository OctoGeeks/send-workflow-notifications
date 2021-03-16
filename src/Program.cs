using DotnetActionsToolkit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SendWorkflowNotifications.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SendWorkflowNotifications
{
    class Program
    {
        static readonly Core _core = new Core();

        static void Main(string[] args)
        {
            foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
            {
                Console.WriteLine($"{envVar.Key}: {envVar.Value}");
            }

            var smtpServer = _core.GetInput("smtp-server");
            var smtpUser = _core.GetInput("smtp-user");
            var smtpPassword = _core.GetInput("smtp-password");
            var configFile = _core.GetInput("configuration");

            var eventFile = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");

            var config = GetConfiguration(configFile);
            var workflowInfo = GetWorkflow(eventFile);

            var messages = GenerateEmails(workflowInfo, config);
            SendEmails(messages, smtpServer, smtpUser, smtpPassword);
        }

        private static IEnumerable<MimeMessage> GenerateEmails(WorkflowInfo workflowInfo, NotificationsConfiguration config)
        {
            var workflowConfig = config.workflows.FirstOrDefault(w => w.workflow == workflowInfo.WorkflowName);

            if (workflowConfig != null)
            {
                var notifications = workflowConfig.notifications.Where(n => DoesFilterMatch(n.filter, workflowInfo));

                foreach (var notification in notifications)
                {
                    _core.Info($"Sending notification to {notification.email}");
                    yield return GenerateEmail(notification.email, workflowInfo);
                }
            }
        }

        private static MimeMessage GenerateEmail(string email, WorkflowInfo workflowInfo)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("GitHub Notifications", "notifications@github.com"));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = $"[{workflowInfo.RepoSlug}] Run {workflowInfo.Status}: {workflowInfo.WorkflowName}";

            var builder = new BodyBuilder();

            var templatePath = Path.Combine(AppContext.BaseDirectory, "../../emailTemplate.html");
            builder.HtmlBody = File.ReadAllText(templatePath);

            builder.HtmlBody = builder.HtmlBody.Replace("$$REPO_SLUG$$", workflowInfo.RepoSlug)
                                               .Replace("$$WORKFLOW_NAME$$", workflowInfo.WorkflowName)
                                               .Replace("$$WORKFLOW_RUN_LINK$$", workflowInfo.Url)
                                               .Replace("$$WORKFLOW_STATUS$$", workflowInfo.Status.ToString());

            message.Body = builder.ToMessageBody();

            return message;
        }

        private static bool DoesFilterMatch(string[] filter, WorkflowInfo workflowInfo)
        {
            var filters = filter.Select(f => f.ToLowerInvariant()).ToList();

            if (workflowInfo.Status == WorkflowStatus.Requested && filters.Contains("requested"))
            {
                return true;
            }

            if (workflowInfo.Status == WorkflowStatus.Succeeded && filters.Contains("succeeded"))
            {
                return true;
            }

            if (workflowInfo.Status == WorkflowStatus.Failed && filters.Contains("failed"))
            {
                return true;
            }

            return false;
        }

        private static void SendEmails(IEnumerable<MimeMessage> messages, string smtpServer, string smtpUser, string smtpPassword)
        {
            using (var client = new SmtpClient())
            {
                client.Connect(smtpServer, 465, SecureSocketOptions.SslOnConnect);

                client.Authenticate(smtpUser, smtpPassword);

                foreach (var msg in messages)
                {
                    client.Send(msg);
                }

                client.Disconnect(true);
            }
        }

        private static WorkflowInfo GetWorkflow(string eventFile)
        {
            var result = new WorkflowInfo();

            if (!File.Exists(eventFile))
            {
                Console.Error.WriteLine($"ERROR: Cannot find event file: {eventFile}");
                throw new FileNotFoundException();
            }

            var eventJson = File.ReadAllText(eventFile);

            var jsonDoc = JsonDocument.Parse(eventJson);

            result.WorkflowName = jsonDoc.RootElement.GetProperty("workflow_run").GetProperty("name").GetString();
            result.Url = jsonDoc.RootElement.GetProperty("workflow_run").GetProperty("html_url").GetString();

            var action = jsonDoc.RootElement.GetProperty("action").GetString();

            if (action == "requested")
            {
                result.Status = WorkflowStatus.Requested;
            }
            else
            {
                var conclusion = jsonDoc.RootElement.GetProperty("workflow_run").GetProperty("conclusion").GetString();

                if (conclusion == "success")
                {
                    result.Status = WorkflowStatus.Succeeded;
                }
                else
                {
                    result.Status = WorkflowStatus.Failed;
                }
            }

            var orgName = jsonDoc.RootElement.GetProperty("organization").GetProperty("login").GetString();
            var repoName = jsonDoc.RootElement.GetProperty("repository").GetProperty("name").GetString();

            result.RepoSlug = $"{orgName}/{repoName}";

            _core.Info($"Processing notifications for Workflow: {result.WorkflowName} [{result.Status}]...");

            return result;
        }

        private static NotificationsConfiguration GetConfiguration(string configFile)
        {
            if (!File.Exists(configFile))
            {
                Console.Error.WriteLine($"ERROR: Cannot find configuration file: {configFile}");
                throw new FileNotFoundException();
            }

            var input = File.ReadAllText(configFile);
            var deserializer = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();

            return deserializer.Deserialize<NotificationsConfiguration>(input);
        }
    }
}
