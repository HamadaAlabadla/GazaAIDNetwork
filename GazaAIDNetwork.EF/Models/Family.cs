using System.ComponentModel.DataAnnotations;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.EF.Models
{
    public class Family
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string HusbandId { get; set; }

        [Required]
        public User Husband { get; set; }

        public int _numberMembers = 0;
        public int NumberMembers
        {
            get => _numberMembers;
            set => _numberMembers = value < 0 ? 0 : value;
        }

        [Required]
        public MemberStatus HusbandStatus { get; set; }

        public DateTime DateChangeStatusForHusband { get; set; }

        [Required]
        public Gender GenderForHusband { get; set; }

        [StringLength(100, ErrorMessage = "اسم الزوجة لا يجب ان يزيد عن 100 حرف")]
        public string? WifeName { get; set; }

        [RegularExpression(@"^\d{9}$", ErrorMessage = "رقم الهوية يجب أن يتكون من 9 أرقام")]
        public string? WifeIdNumber { get; set; }

        [Required]
        public MemberStatus WifeStatus { get; set; }

        public DateTime DateChangeStatusForWife { get; set; }

        [Required]
        public Gender GenderForWife { get; set; }

        [Required]
        public MaritalStatus MaritalStatus { get; set; }

        [Required]
        public FinancialSituation FinancialSituation { get; set; }

        [Required]
        public string RepresentativeId { get; set; }

        [Required]
        public User Representative { get; set; }
        public StatusFamily StatusFamily { get; set; } = StatusFamily.noRequest;
        [Required]
        public Guid OriginalAddressId { get; set; }  // Required Address

        [Required]
        public Address OriginalAddress { get; set; } // One-to-One Relationship (Required)

        public bool IsDeleted { get; set; } = false;
        public bool IsPledge { get; set; } = false;

        public string DisplaceId { get; set; }
        public Displace Displace { get; set; }
        public Disability Disability { get; set; }
        public Disease Disease { get; set; }
        public ICollection<OrderAid> OrderAids { get; set; }
    }
}
