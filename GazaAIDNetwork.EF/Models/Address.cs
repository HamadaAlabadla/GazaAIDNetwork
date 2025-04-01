using System.ComponentModel.DataAnnotations;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.EF.Models
{
    public class Address
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Governotate Governotate { get; set; }

        [Required]
        public City City { get; set; }

        [Required]
        public Neighborhood Neighborhood { get; set; }
        public ICollection<Family> FamiliesWithOriginalAddress { get; set; } = new List<Family>(); // One-to-Many

    }
}
