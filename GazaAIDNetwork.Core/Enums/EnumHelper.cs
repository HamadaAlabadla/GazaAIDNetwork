using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GazaAIDNetwork.Core.Enums
{
    public static class EnumHelper
    {
        public static string GetDisplayName(Enum value)
        {
            return value.GetType()
                        .GetMember(value.ToString())
                        .FirstOrDefault()?
                        .GetCustomAttribute<DisplayAttribute>()?
                        .Name ?? value.ToString();
        }
    }
}
