using System.ComponentModel.DataAnnotations.Schema;

namespace dotnetweb.Models;

public class Transaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    [ForeignKey("WalletId")]
    public Wallet Wallet { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // Credit, Debit
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
}
