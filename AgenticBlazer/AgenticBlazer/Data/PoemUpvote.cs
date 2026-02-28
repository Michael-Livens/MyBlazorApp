namespace AgenticBlazer.Data;

public class PoemUpvote
{
    public int Id { get; set; }
    public int PoemId { get; set; }
    public Poem? Poem { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
}
