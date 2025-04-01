using System.ComponentModel.DataAnnotations;

namespace GazaAIDNetwork.Core.Dtos
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        [Required(ErrorMessage = "رقم الهوية مطلوب")]
        [Display(Name = "رقم الهوية")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "رقم الهوية يجب أن يتكون من 9 أرقام")]
        public string IdNumber { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Display(Name = "رقم الهاتف")]
        [RegularExpression(@"^(\+972|972|0)?5[0-9]{8}$", ErrorMessage = "رقم الهاتف غير صحيح")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; }
        public string DivisionId { get; set; }
        public int[] Roles { get; set; } = [];
    }
}
