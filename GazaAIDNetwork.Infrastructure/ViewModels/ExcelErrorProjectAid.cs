namespace GazaAIDNetwork.Infrastructure.ViewModels
{
    public class ExcelErrorProjectAid
    {
        public int RowNumber { get; set; }
        public string IdNumber { get; set; }
        public string Name { get; set; }
        public string ErrorMessage { get; set; }

        public ExcelErrorProjectAid(int rowNumber, string idNumber, string name, string errorMessage)
        {
            RowNumber = rowNumber;
            IdNumber = idNumber;
            Name = name;
            ErrorMessage = errorMessage;
        }
    }

}
