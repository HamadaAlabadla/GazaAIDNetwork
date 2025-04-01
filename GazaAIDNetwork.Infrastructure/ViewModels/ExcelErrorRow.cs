using GazaAIDNetwork.Core.Dtos;

namespace GazaAIDNetwork.Infrastructure.ViewModels
{
    public class ExcelErrorRow
    {
        public int RowNumber { get; set; }
        public FamilyDto Family { get; set; }
        public string ErrorMessage { get; set; }

        public ExcelErrorRow(int rowNumber, FamilyDto family, string errorMessage)
        {
            RowNumber = rowNumber;
            Family = family;
            ErrorMessage = errorMessage;
        }
    }

}
