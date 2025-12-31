using dotnetweb.Data;
using dotnetweb.DTOs;
using dotnetweb.Models;
using dotnetweb.Services;
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
    private readonly TransactionExportService _exportService;

    public WalletController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
        _exportService = new TransactionExportService();
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
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
            Category = "Salary",
            Date = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? dbEx.Message;
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Database error while saving deposit", new List<string> { inner }));
        }
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
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
            Category = "Withdrawal",
            Date = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? dbEx.Message;
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Database error while saving withdrawal", new List<string> { inner }));
        }
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
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
                Category = "Transfer",
                Date = DateTime.UtcNow
            };

            var creditTx = new Transaction
            {
                WalletId = receiverWallet.Id,
                Amount = model.Amount,
                Type = "Credit",
                Description = $"Transfer from {senderWallet.User.Email}",
                Category = "Transfer",
                Date = DateTime.UtcNow
            };

            _context.Transactions.Add(debitTx);
            _context.Transactions.Add(creditTx);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                NewBalance = senderWallet.Balance,
                TransactionId = debitTx.Id,
                ReceiverEmail = model.ReceiverEmail,
                Amount = model.Amount
            }, "Transfer successful"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred during transfer", new List<string> { ex.Message }));
        }
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? category = null)
    {
        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        var query = _context.Transactions.Where(t => t.WalletId == wallet.Id);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category.ToLower() == category.ToLower());
        }

        var totalCount = await query.CountAsync();

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                Date = t.Date,
                Description = t.Description,
                Category = t.Category
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
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
                Description = t.Description,
                Category = t.Category
            })
            .FirstOrDefaultAsync();

        if (transaction == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Transaction not found"));

        return Ok(ApiResponse<object>.SuccessResponse(transaction, "Transaction retrieved successfully"));
    }

    [HttpGet("transactions/export/csv")]
    public async Task<IActionResult> ExportTransactionsCsv([FromQuery] string? category = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        var query = _context.Transactions.Where(t => t.WalletId == wallet.Id);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category.ToLower() == category.ToLower());

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                Date = t.Date,
                Description = t.Description,
                Category = t.Category
            })
            .ToListAsync();

        if (transactions.Count == 0)
            return NotFound(ApiResponse<object>.ErrorResponse("No transactions found for export"));

        var csvData = _exportService.ExportToCsv(transactions);
        return File(csvData, "text/csv", $"transactions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    [HttpGet("transactions/export/pdf")]
    public async Task<IActionResult> ExportTransactionsPdf([FromQuery] string? category = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        var query = _context.Transactions.Where(t => t.WalletId == wallet.Id);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category.ToLower() == category.ToLower());

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                Date = t.Date,
                Description = t.Description,
                Category = t.Category
            })
            .ToListAsync();

        if (transactions.Count == 0)
            return NotFound(ApiResponse<object>.ErrorResponse("No transactions found for export"));

        var userName = !string.IsNullOrEmpty(user.FullName) ? user.FullName : (user.Email ?? "User");
        var pdfData = _exportService.ExportToPdf(transactions, userName);
        return File(pdfData, "application/pdf", $"transactions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }

    [HttpGet("transactions/categories")]
    public async Task<IActionResult> GetCategories()
    {
        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        var categories = await _context.Transactions
            .Where(t => t.WalletId == wallet.Id)
            .Select(t => t.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(categories, "Categories retrieved successfully"));
    }

    [HttpGet("transactions/summary/by-category")]
    public async Task<IActionResult> GetTransactionsSummaryByCategory([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var userId = User.FindFirst("id")?.Value;
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Wallet not found"));

        var query = _context.Transactions.Where(t => t.WalletId == wallet.Id);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        var summary = await query
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                Category = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                DebitsCount = g.Count(t => t.Type == "Debit"),
                CreditsCount = g.Count(t => t.Type == "Credit")
            })
            .OrderByDescending(s => s.TotalAmount)
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(summary, "Category summary retrieved successfully"));
    }
}
