namespace Producer;

public class TrackerConfiguration
{
    public TrackerType Type { get; set; }

    public ITrackerConfiguration Configuration { get; set; }
}