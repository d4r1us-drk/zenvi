namespace Zenvi.Models;

public class SearchViewModel
{
    public string Query { get; set; }
    public List<User> Users { get; set; }
    public List<Post> Posts { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}