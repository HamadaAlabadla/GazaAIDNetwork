using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.EF.Models
{
    public class OrderAid
    {
        public Guid Id { get; set; }
        public int Quantity { get; set; }
        public Guid FamilyId { get; set; }
        public Family Family { get; set; }
        public ProjectAid ProjectAid { get; set; }
        public Guid ProjectAidId { get; set; }
        public OrderAidStatus OrderAidStatus { get; set; } = OrderAidStatus.Pending;
    }
}
