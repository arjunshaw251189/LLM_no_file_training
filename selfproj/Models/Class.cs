namespace selfproj.Models
{
    
    public class innervalues
    {
        public string name { get; set; }
        public string value { get; set; }
        public string unit { get; set; }
        public string symbol { get; set; }
    }
    public class openapireturn
    {
        public string subject { get; set; }
        public string attribute { get; set; }
        public bool comparative { get; set; }
        public string detectedlanguage { get; set; }
    }
    public class answer_return : openapireturn
    {
        public List<string> answer { get; set; }
    }
}
