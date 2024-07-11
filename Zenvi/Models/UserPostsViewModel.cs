namespace Zenvi.Models
{
    public class UserPostsViewModel
    {
        public User User { get; set; }
        public List<Post> Posts { get; set; }
        public List<int> LikedPostIds { get; set; }
    }
}
