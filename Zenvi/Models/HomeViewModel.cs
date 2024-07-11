namespace Zenvi.Models;

public class HomeViewModel
{
    public User User { get; set; }
    public List<Post> Posts { get; set; }
    public HashSet<int> LikedPostIds { get; set; }
}