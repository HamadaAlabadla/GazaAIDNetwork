using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.EF.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public EntityType EntityType { get; set; }
        public string RepoId { get; set; }
        public AuditName Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string AdminId { get; set; }
        public virtual User Admin { get; set; }
    }
}
