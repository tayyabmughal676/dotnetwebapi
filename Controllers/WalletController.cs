using dotnetweb.Data;
using dotnetweb.DTOs;
using dotnetweb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnetweb.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public WalletController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        return Ok(ApiResponse<object>.SuccessResponse(new { 
            Balance = wallet.Balance, 
            Currency = wallet.Currency 
        }, "Balance retrieved successfully"));
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositDto model)
    {
        if (model.Amount <= 0) 
            return BadRequest(ApiResponse<object>.ErrorResponse("Amount must be positive"));

        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        wallet.Balance += model.Amount;
        
        var transaction = new Transaction
        {
            WalletId = wallet.Id,
            Amount = model.Amount,
            Type = "Credit",
            Description = "Deposit",
            Date = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { 
            NewBalance = wallet.Balance,
            TransactionId = transaction.Id
        }, "Deposit successful"));
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawDto model)
    {
        if (model.Amount <= 0) 
            return BadRequest(ApiResponse<object>.ErrorResponse("Amount must be positive"));

        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        if (wallet.Balance < model.Amount) 
            return BadRequest(ApiResponse<object>.ErrorResponse("Insufficient funds"));

        wallet.Balance -= model.Amount;
        
        var transaction = new Transaction
        {
            WalletId = wallet.Id,
            Amount = model.Amount,
            Type = "Debit",
            Description = "Withdrawal",
            Date = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { 
            NewBalance = wallet.Balance,
            TransactionId = transaction.Id
        }, "Withdrawal successful"));
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferDto model)
    {
        if (model.Amount <= 0) 
            return BadRequest(ApiResponse<object>.ErrorResponse("Amount must be positive"));

        var userId = User.FindFirst("id")?.Value;
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var senderWallet = await _context.Wallets.Include(w => w.User).FirstOrDefaultAsync(w => w.UserId == userId);
            
            if (senderWallet == null) 
                return NotFound(ApiResponse<object>.ErrorResponse("Sender wallet not found"));
            if (senderWallet.Balance < model.Amount) 
                return BadRequest(ApiResponse<object>.ErrorResponse("Insufficient funds"));

            var receiver = await _userManager.FindByEmailAsync(model.ReceiverEmail);
            if (receiver == null) 
                return NotFound(ApiResponse<object>.ErrorResponse("Receiver not found"));
            
            if (receiver.Id == senderWallet.UserId) 
                return BadRequest(ApiResponse<object>.ErrorResponse("Cannot transfer to yourself"));

            var receiverWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == receiver.Id);
            if (receiverWallet == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Receiver wallet not found"));
            }

            senderWallet.Balance -= model.Amount;
            receiverWallet.Balance += model.Amount;

            var debitTx = new Transaction
            {
                WalletId = senderWallet.Id,
                Amount = model.Amount,
                Type = "Debit",
                Description = $"Transfer to {model.ReceiverEmail}",
                Date = DateTime.UtcNow
            };
            
            var creditTx = new Transaction
            {
                WalletId = receiverWallet.Id,
                Amount = model.Amount,
                Type = "Credit",
                Description = $"Transfer from {senderWallet.User.Email}",
                Date = DateTime.UtcNow
            };

            _context.Transactions.Add(debitTx);
            _context.Transactions.Add(creditTx);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return Ok(ApiResponse<object>.SuccessResponse(new { 
                NewBalance = senderWallet.Balance,
                TransactionId = debitTx.Id,
                ReceiverEmail = model.ReceiverEmail,
                Amount = model.Amount
            }, "Transfer successful"));
        }
        catch(Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred during transfer", new List<string> { ex.Message }));
        }
    }
    
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        var totalCount = await _context.Transactions.CountAsync(t => t.WalletId == wallet.Id);
        
        var transactions = await _context.Transactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                Date = t.Date,
                Description = t.Description
            })
            .ToListAsync();
            
        return Ok(ApiResponse<object>.SuccessResponse(new {
            Transactions = transactions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        }, "Transactions retrieved successfully"));
    }

    [HttpGet("transactions/{id}")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) 
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        var transaction = await _context.Transactions
            .Where(t => t.WalletId == wallet.Id && t.Id == id)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                Date = t.Date,
                Description = t.Description
            })
            .FirstOrDefaultAsync();

        if (transaction == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Transaction not found"));
            
        return Ok(ApiResponse<object>.SuccessResponse(transaction, "Transaction retrieved successfully"));
    }
}
