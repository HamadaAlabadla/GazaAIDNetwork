using System.ComponentModel.DataAnnotations;

namespace GazaAIDNetwork.EF.Models
{
    public class Displace
    {
        [Key]
        public Guid Id { get; set; }

        public Guid FamilyId { get; set; }

        public Family Family { get; set; } // علاقة مع Family

        [Required]
        public Guid CurrentAddressId { get; set; } // The new address

        [Required]
        public Address CurrentAddress { get; set; } // The family's current location
        public bool IsDeleted { get; set; } = false;
    }
}
