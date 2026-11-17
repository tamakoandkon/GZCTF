using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// Signature verification
/// </summary>
public class SignatureVerifyModel
{
    /// <summary>
    /// Team token
    /// </summary>
    [Required]
    [MaxLength(100)] // Signed 32-bit ID, separator and a Base64 Ed25519 signature.
    public string TeamToken { get; set; } = string.Empty;

    /// <summary>
    /// Game public key, Base64 encoded
    /// </summary>
    [Required]
    [StringLength(44, MinimumLength = 44)]
    public string PublicKey { get; set; } = string.Empty;
}
