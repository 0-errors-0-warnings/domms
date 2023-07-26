namespace CalcEngineService.DTO;

public class UnderlierConfigSet
{
    public string? Underlier { get; set; }

    public double SpotRangePct { get; set; }
    public double SpotStepSizePct { get; set; }
    public int SpotStride { get; set; }

    public double VolRangePct { get; set; }
    public double VolStepSizePct { get; set; }
    public int VolStride { get; set; }

    public double RateRangePct { get; set; }
    public double RateStepSizePct { get; set; }
    public int RateStride { get; set; }

    public double DivRangePct { get; set; }
    public double DivStepSizePct { get; set; }
    public int DivStride { get; set; }

    public double TimeRangeMins { get; set; }
    public double TimeStepSizeMins { get; set; }
    public int TimeStride { get; set; }
}
