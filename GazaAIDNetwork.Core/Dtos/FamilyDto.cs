using System.ComponentModel.DataAnnotations;
using static GazaAIDNetwork.Core.Enums.Enums;

namespace GazaAIDNetwork.Core.Dtos
{
    public class FamilyDto : IValidatableObject
    {
        public string Id { get; set; }
        // For User Class
        [Required(ErrorMessage = "اسم الزوج مطلوب")]
        public string HusbandName { get; set; }

        [Required(ErrorMessage = "رقم الهوية مطلوب")]
        [Display(Name = "رقم الهوية")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "رقم الهوية يجب أن يتكون من 9 أرقام")]
        public string IdNumber { get; set; }
        [Display(Name = "رقم الهاتف")]
        [RegularExpression(@"^(\+972|972|0)?5[0-9]{8}$", ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        //For Family Class

        [Range(0, 20, ErrorMessage = "يجب أن تكون القيمة 0 أو أكثر")]
        public int NumberMembers { get; set; }

        [Required(ErrorMessage = "حالة الزوج مطلوبة")]
        public MemberStatus HusbandStatus { get; set; }

        public DateTime DateChangeStatusForHusband { get; set; }

        [Required(ErrorMessage = "جنس الزوج مطلوب")]
        public Gender GenderForHusband { get; set; }

        [StringLength(100, ErrorMessage = "يجب ألا يزيد اسم الزوجة عن 100 حرف")]
        public string WifeName { get; set; }

        [RegularExpression(@"^\d{9}$", ErrorMessage = "رقم هوية الزوجة يجب أن يتكون من 9 أرقام")]
        public string? WifeIdNumber { get; set; }

        public MemberStatus WifeStatus { get; set; }

        public DateTime DateChangeStatusForWife { get; set; }

        public Gender GenderForWife { get; set; }

        [Required(ErrorMessage = "الحالة الاجتماعية مطلوبة")]
        public MaritalStatus MaritalStatus { get; set; }

        [Required(ErrorMessage = "الوضع المالي مطلوب")]
        public FinancialSituation FinancialSituation { get; set; }
        public StatusFamily StatusFamily { get; set; }
        public string DivisionName { get; set; }
        public string RepresentativeId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsPledge { get; set; } = false;

        // For Address Class
        [Required(ErrorMessage = "المحافظة مطلوبة")]
        public Governotate OriginalGovernotate { get; set; }

        [Required(ErrorMessage = "اختيار المدينة مطلوب")]
        public City OriginaCity { get; set; }

        [Required(ErrorMessage = "اختيار الحي مطلوب")]

        public Neighborhood OriginaNeighborhood { get; set; }


        // For Displace Class "Address"
        public bool IsDisplaced { get; set; }
        public Governotate? CurrentGovernotate { get; set; }
        public City? CurrentCity { get; set; }
        public Neighborhood? CurrentNeighborhood { get; set; }


        // For Disability Class

        public bool IsDisability { get; set; }

        //[Range(0, 20, ErrorMessage = "يجب أن تكون القيمة 0 أو أكثر")]
        public int? Mental { get; set; } = 0;

        //[Range(0, 20, ErrorMessage = "يجب أن تكون القيمة 0 أو أكثر")]
        public int? Motor { get; set; } = 0;

        //[Range(0, 20, ErrorMessage = "يجب أن تكون القيمة 0 أو أكثر")]
        public int? Hearing { get; set; } = 0;

        //[Range(0, 20, ErrorMessage = "يجب أن تكون القيمة 0 أو أكثر")]
        public int? Visual { get; set; } = 0;


        // For Disease Class

        public bool IsDisease { get; set; }

        //[Range(0, 20, ErrorMessage = "يجب أن تكون القيمة 0 أو أكثر")]
        public int? Diabetes { get; set; } = 0;

        //[Range(0, 20, ErrorMessage = "يجب أن تكون القيمة 0 أو أكثر")]
        public int? BloodPressure { get; set; } = 0;

        //[Range(0, 20, ErrorMessage = "يجب أن تكون القيمة 0 أو أكثر")]
        public int? Cancer { get; set; } = 0;

        //[Range(0, 20, ErrorMessage = "يجب أن تكون القيمة 0 أو أكثر")]
        public int? KidneyFailure { get; set; } = 0;


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsDisability)
            {
                if (Mental < 0 || Motor < 0 || Hearing < 0 || Visual < 0)
                    yield return new ValidationResult("قيم الإعاقة يجب أن تكون 0 أو أكثر", new[] { nameof(Mental), nameof(Motor), nameof(Hearing), nameof(Visual) });
            }

            if (IsDisease)
            {
                if (Diabetes < 0 || BloodPressure < 0 || Cancer < 0 || KidneyFailure < 0)
                    yield return new ValidationResult("قيم الأمراض يجب أن تكون 0 أو أكثر", new[] { nameof(Diabetes), nameof(BloodPressure), nameof(Cancer), nameof(KidneyFailure) });
            }

            if (IsDisplaced)
            {
                if (CurrentGovernotate == null)
                    yield return new ValidationResult("يجب اختيار المحافظة", new[] { nameof(CurrentGovernotate) });

                if (CurrentCity == null)
                    yield return new ValidationResult("يجب اختيار المدينة", new[] { nameof(CurrentCity) });

                if (CurrentNeighborhood == null)
                    yield return new ValidationResult("يجب اختيار الحي", new[] { nameof(CurrentNeighborhood) });
            }

        }
    }
}
