namespace GazaAIDNetwork.Infrastructure.Respons
{
    public class ResultResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object? result { get; set; }
        public IEnumerable<string>? Errors { get; set; }
    }
}
