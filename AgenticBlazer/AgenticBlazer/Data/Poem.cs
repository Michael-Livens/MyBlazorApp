namespace AgenticBlazer.Data;

public class Poem
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSubmitted { get; set; } = false;
    
    public ICollection<PoemUpvote> Upvotes { get; set; } = new List<PoemUpvote>();
}
