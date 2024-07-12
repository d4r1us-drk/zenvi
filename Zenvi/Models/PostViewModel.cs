namespace Zenvi.Models;

public class PostViewModel
{
    public Post Post { get; set; }
    public List<Post> Replies { get; set; }
    public List<int> LikedPostIds { get; set; }
    public string ReplyContent { get; set; }
    public List<IFormFile> ReplyMediaFiles { get; set; }
}