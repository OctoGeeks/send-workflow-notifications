using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendWorkflowNotifications.Configuration
{
    public class WorkflowConfiguration
    {
        public string workflow { get; set; }
        public WorkflowNotificationConfiguration[] notifications { get; set; }
    }
}
