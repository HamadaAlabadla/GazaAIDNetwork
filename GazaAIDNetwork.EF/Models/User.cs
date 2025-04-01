using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace GazaAIDNetwork.EF.Models
{
    public class User : IdentityUser
    {
        [Required(ErrorMessage = "رقم الهوية مطلوب")]
        [Display(Name = "رقم الهوية")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "رقم الهوية يجب أن يتكون من 9 أرقام")]
        public string IdNumber { get; set; }
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Display(Name = "رقم الهاتف")]
        [RegularExpression(@"^(\+972|972|0)?5[0-9]{8}$", ErrorMessage = "رقم الهاتف غير صحيح")]
        public override string PhoneNumber { get; set; }
        public bool isDelete { get; set; } = false;
        public Guid? DivisionId { get; set; }
        public virtual Division Division { get; set; }
    }
}
