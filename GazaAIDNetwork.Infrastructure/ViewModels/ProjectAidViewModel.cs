namespace GazaAIDNetwork.Infrastructure.ViewModels
{
    public class ProjectAidViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Descreption { get; set; }
        public string? Notes { get; set; }
        public string DateCreate { get; set; }
        public string ContinuingUntil { get; set; }
        public int Quantity { get; set; }
        public string DivisionName { get; set; }
        public string ProjectAidStatus { get; set; }
        public List<string> RepresentativeNames { get; set; } = new List<string>();
        public int NumberFamilies { get; set; }

    }
}
