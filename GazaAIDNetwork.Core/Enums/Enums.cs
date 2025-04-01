using System.ComponentModel.DataAnnotations;

namespace GazaAIDNetwork.Core.Enums
{
    public static class Enums
    {
        public enum AuditName
        {
            [Display(Name = "إضافة")]
            Create,
            [Display(Name = "حذف")]
            Delete,
            [Display(Name = "تعديل")]
            Update,
            [Display(Name = "إعادة تفعيل")]
            ReActive,
            [Display(Name = "مقبول")]
            Accepted,
            [Display(Name = "مرفوض")]
            Rejected,
        }
        public enum EntityType
        {
            User,
            Division,
            Family,
            CycleAid,
            InfoRepresentative,
            ProjectAid,
            OrderAid
        }
        public enum Role
        {
            [Display(Name = "مسؤول عام")]
            superadmin = 10,
            [Display(Name = "مسؤول")]
            admin = 11,
            [Display(Name = "مدير")]
            manager = 15,
            [Display(Name = "مندوب")]
            representative = 12,
            [Display(Name = "مراقب")]
            supervisor = 13,
            [Display(Name = "عائلة")]
            family = 14
        }
        public enum Gender
        {
            [Display(Name = "ذكر")]
            Male,
            [Display(Name = "أنثى")]
            Female
        }

        public enum MaritalStatus
        {
            [Display(Name = "متزوج")]
            Married,
            [Display(Name = "مطلق/ة")]
            Divorced,
            [Display(Name = "أرمل/ة")]
            Widowed,
            [Display(Name = "غير متزوجة")]
            Single,
            [Display(Name = "زوجة ثانية")]
            SecondWife,
            [Display(Name = "مسافر")]
            traveler

        }
        public enum FinancialSituation
        {
            [Display(Name = "غير محدد")]
            NotSelected,
            [Display(Name = "منخفض")]
            LowIncome,
            [Display(Name = "منخفض جدا")]
            VeryLowIncome,
            [Display(Name = "متوسط")]
            MiddleIncome,
            [Display(Name = "مرتفع")]
            HighIncome,

        }
        public enum MemberStatus
        {
            [Display(Name = "على قيد الحياة")]
            alive,
            [Display(Name = "متوفى")]
            deceased,
            [Display(Name = "شهيد")]
            Martyr,
            [Display(Name = "معتقل")]
            datained,
            [Display(Name = "مفقود")]
            missing,
            [Display(Name = "مسافر")]
            traveler,
        }
        public enum Governotate
        {
            Kanyounis,
            other
        }
        public enum City
        {
            AlQarara,
            other
        }
        public enum Neighborhood
        {
            WesternRegion,
            CentralRegion,
            EasternRegion,
            NorthernRegion,
            SouthernRegion,
            MawasiAlQarara

        }
        public enum StatusFamily
        {
            [Display(Name = "لم يتم تقديم طلب")]
            noRequest = 3,
            [Display(Name = "تمت الموافقة")]
            accepted = 4,
            [Display(Name = "بإنتظار التوقيع من المندوب")]
            pending = 5,
            [Display(Name = "تم الرفض من المندوب , راجع لمعرفة التفاصيل")]
            rejected = 6,
        }

        public enum ProjectAidStatus
        {
            [Display(Name = "قيد التجهيز")]
            UnderPreparation = 4,
            [Display(Name = "تم الإعتماد")]
            Confirmed = 5,
            [Display(Name = "تم الإنتهاء")]
            Done = 6
        }

        public enum CycleAidStatus
        {
            [Display(Name = "بإنتظار البدء ...")]
            Pending = 4,
            [Display(Name = "تم البدء")]
            Start = 5,
            [Display(Name = "تم الإنتهاء")]
            Done = 6
        }

        public enum OrderAidStatus
        {
            [Display(Name = "بانتظار الموافقة من المسؤول")]
            Pending = 4,
            [Display(Name = "تم الموافقة")]
            Accepted = 5,
            [Display(Name = "تم الرفض")]
            Rejected = 6,
            [Display(Name = "توجه للإستلام")]
            GoToPickUp = 7,
            [Display(Name = "تم الإستلام")]
            Delivered = 8,

        }

    }
}
