namespace CalcEngineService.Configs;

public class MeshConfigSetConfiguration
{
    public string Underlier { get; set; }
    public double SpotRangePct { get; set; }
    public double SpotStepSizePct { get; set; }
    public double VolRangePct { get; set; }
    public double VolStepSizePct { get; set; }
    public double RateRangePct { get; set; }
    public double RateStepSizePct { get; set; }
    public double DivRangePct { get; set; }
    public double DivStepSizePct { get; set; }
    public double TimeRangeMins { get; set; }
    public double TimeStepSizeMins { get; set; }
}
