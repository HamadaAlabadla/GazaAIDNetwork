namespace GazaAIDNetwork.Infrastructure.ViewModels
{
    public class DivisionViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public List<AuditLogViewModel> AuditLogs { get; set; } = new List<AuditLogViewModel>();
    }
}
