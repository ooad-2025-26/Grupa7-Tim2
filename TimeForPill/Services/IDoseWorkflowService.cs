namespace TimeForPill.Services
{
    public interface IDoseWorkflowService
    {
        Task RefreshMissedDosesAsync();
        Task SendDueReminderEmailsAsync();
    }
}
