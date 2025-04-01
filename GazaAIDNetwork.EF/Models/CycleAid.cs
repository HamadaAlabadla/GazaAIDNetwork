using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.EF.Models
{
    public class CycleAid
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid DivisionId { get; set; }
        public Division Division { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; } = null;
        public CycleAidStatus CycleAidStatus { get; set; }
        public ICollection<ProjectAid> ProjectAids { get; set; }
    }
}
