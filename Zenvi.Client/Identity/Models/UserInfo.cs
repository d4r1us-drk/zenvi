using Zenvi.Shared;

namespace Zenvi.Client.Identity.Models;

public class UserInfo : AboutUserDto
{
    public Dictionary<string, string> Claims { get; set; } = [];
}