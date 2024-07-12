namespace Zenvi.Models;

public class UserPostsViewModel
{
    public User User { get; set; }
    public List<Post> Posts { get; set; }
    public List<int> LikedPostIds { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool IsFollowing { get; set; }
}