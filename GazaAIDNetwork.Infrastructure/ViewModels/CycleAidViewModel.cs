namespace GazaAIDNetwork.Infrastructure.ViewModels
{
    public class CycleAidViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DivisionName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; } = null;
        public string CycleAidStatus { get; set; }
        public int NumberOfBeneficiaries { get; set; }
        public int NumberOfFamilies { get; set; }
    }
}
