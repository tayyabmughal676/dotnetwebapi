using System.ComponentModel.DataAnnotations.Schema;

namespace dotnetweb.Models;

public class Wallet
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
}
