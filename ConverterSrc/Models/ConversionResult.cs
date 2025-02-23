namespace LLMSmartConverter.Models
{
    public class ConversionResult
    {
        public string Status { get; set; }
        public string ResultDescription { get; set; }
        public string ConvertDescription { get; set; }
        public ConversionFileResult[] Files { get; set; }
        public string Attention { get; set; }
        public string ConfigFileContent { get; set; }
    }

    public class ConversionFileResult
    {
        public string FileType { get; set; }
        public string FileName { get; set; }
        public string CodeContent { get; set; }
    }
}
