namespace _2cpbackend.Models;

public class Duration
{
    public int Days { get; set; }
    public int Hours { get; set; }
    public int Minutes { get; set; }

    public Duration(TimeSpan timeSpan)
    {
        this.Days = timeSpan.Days;
        this.Hours = timeSpan.Hours;
        this.Minutes = timeSpan.Minutes;
    }
}