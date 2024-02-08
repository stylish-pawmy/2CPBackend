namespace Eventi.Server.Models;

public class EventCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<Event> Events { get; set; } = null!;
}