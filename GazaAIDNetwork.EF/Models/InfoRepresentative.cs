namespace GazaAIDNetwork.EF.Models
{
    public class InfoRepresentative
    {
        public Guid Id { get; set; }
        public string RepresntativeId { get; set; }
        public User Represntative { get; set; }
        public Guid ProjectAidId { get; set; }
        public ProjectAid ProjectAid { get; set; }
        public double Percentage { get; set; } = 0;
        public int Quantity { get; set; }
    }
}
