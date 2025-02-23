namespace LLMSmartConverter.Models
{
    public class ConversionResult
    {
        public string Status { get; set; }
        public string ResultDescription { get; set; }
        public string ConvertDescription { get; set; }
        public string FileRelativePath { get; set; }
        public string CodeContent { get; set; }
        public string Attention { get; set; }
        public string ConfigFileContent { get; set; }
    }
}
