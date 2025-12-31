using Microsoft.AspNetCore.Identity;

namespace dotnetweb.Models;

public class User : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
