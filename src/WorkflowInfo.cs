namespace SendWorkflowNotifications
{
    public class WorkflowInfo
    {
        public string WorkflowName { get; set; }
        public WorkflowStatus Status { get; set; }
        public string Url { get; set; }
        public string RepoSlug { get; set; }
    }
}
