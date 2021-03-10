using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendWorkflowNotifications.Configuration
{
    public class WorkflowNotificationConfiguration
    {
        public string email { get; set; }
        public string[] filter { get; set; }
    }
}
