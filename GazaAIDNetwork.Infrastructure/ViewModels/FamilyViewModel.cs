using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Infrastructure.ViewModels
{
    public class FamilyViewModel
    {
        public string Id { get; set; }
        public string HusbandName { get; set; }
        public string IdNumber { get; set; }
        public string PhoneNumber { get; set; }
        public int NumberMembers { get; set; }
        public string HusbandStatus { get; set; }
        public string DateChangeStatusForHusband { get; set; }
        public string GenderForHusband { get; set; }
        public string WifeName { get; set; }
        public string WifeIdNumber { get; set; }
        public string WifeStatus { get; set; }
        public string DateChangeStatusForWife { get; set; }
        public string GenderForWife { get; set; }
        public string MaritalStatus { get; set; }
        public string FinancialSituation { get; set; }
        public string RepresentativeName { get; set; }
        public Governotate OriginalGovernotate { get; set; }
        public City OriginaCity { get; set; }
        public Neighborhood OriginaNeighborhood { get; set; }
        public string StatusFamily { get; set; }
    }
}
