using System.ComponentModel.DataAnnotations.Schema;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.EF.Models
{
    public class ProjectAid
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Descreption { get; set; }
        public string? Notes { get; set; }
        public DateTime DateCreate { get; set; }
        public DateTime ContinuingUntil { get; set; }
        public int Quantity { get; set; }
        public Guid DivisionId { get; set; }
        public Division Division { get; set; }
        public Guid? CycleAidId { get; set; }
        public CycleAid? CycleAid { get; set; }
        public ProjectAidStatus ProjectAidStatus { get; set; }
        [NotMapped]
        public List<string> RepresentativeIds { get; set; }
        public ICollection<InfoRepresentative> InfoRepresentatives { get; set; } = new HashSet<InfoRepresentative>();
        public ICollection<OrderAid> OrderAids { get; set; } = new HashSet<OrderAid>();
    }
}
