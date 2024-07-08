using System.ComponentModel.DataAnnotations;

namespace Zenvi.Shared;

public class FollowDto
{
    [Required]
    public string TargetUserId { get; set; }
}

public class FollowerDto
{
    public string FollowerUserName { get; set; }
    public string FollowerName { get; set; }
    public string FollowerUserId { get; set; }
    public DateTime FollowedAt { get; set; }
}

public class FollowingDto
{
    public string FollowingUserName { get; set; }
    public string FollowingName { get; set; }
    public string FollowingUserId { get; set; }
    public DateTime FollowedAt { get; set; }
}
