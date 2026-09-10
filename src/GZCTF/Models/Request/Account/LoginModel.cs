using System.ComponentModel.DataAnnotations;
using GZCTF.Services;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Login
/// </summary>
public class LoginModel : ModelWithCaptcha
{
    /// <summary>
    /// Username or email
    /// </summary>
    [Required]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxPasswordLength)]
    public string Password { get; set; } = string.Empty;
}
