using CsvHelper.Configuration.Attributes;

namespace ImpliciX.Designer.Simulation
{
    internal class Line
    {
        [Index(0)]
        public string At { get; set; }
        [Index(1)]
        public string Type { get; set; }
        [Index(2)]
        public string Argument { get; set; }
        [Index(3)]
        public string Value { get; set; }
    }
}