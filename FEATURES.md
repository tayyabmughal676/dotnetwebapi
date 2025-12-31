# New Features Documentation

## 1. Category-Based Expense Tracking

### Overview
Transactions now support categorization to help users organize and analyze their expenses by type.

### Implementation Details

#### Transaction Categories
The following default categories are available:
- **Salary** - Income transactions (deposits)
- **Transfer** - Money transfers between users
- **Withdrawal** - Withdrawals from wallet
- **Other** - Default category for uncategorized transactions
- **Food** - Food and dining expenses
- **Entertainment** - Entertainment expenses
- **Transport** - Transportation costs
- **Utilities** - Utility bills and services

#### Database Changes
- Added `Category` field to the `Transaction` model (nullable string, defaults to "Other")
- Migration: `20251231144409_AddTransactionCategory`

#### API Endpoints

##### Get All Categories
```
GET /api/wallet/transactions/categories
Authorization: Bearer <token>
```
Returns list of all unique categories used in user's transactions.

**Response:**
```json
{
  "success": true,
  "data": [
    "Food",
    "Entertainment",
    "Other",
    "Salary",
    "Transfer",
    "Withdrawal"
  ],
  "message": "Categories retrieved successfully"
}
```

##### Get Transactions with Category Filter
```
GET /api/wallet/transactions?page=1&pageSize=10&category=Food
Authorization: Bearer <token>
```

**Query Parameters:**
- `page` - Page number (default: 1)
- `pageSize` - Items per page (default: 10)
- `category` - Filter by category (optional)

**Response:**
```json
{
  "success": true,
  "data": {
    "transactions": [
      {
        "id": 1,
        "amount": 25.50,
        "type": "Debit",
        "date": "2025-12-31T10:30:00Z",
        "description": "Lunch",
        "category": "Food"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 10,
    "totalPages": 1
  },
  "message": "Transactions retrieved successfully"
}
```

##### Get Transaction Summary by Category
```
GET /api/wallet/transactions/summary/by-category?startDate=2025-12-01&endDate=2025-12-31
Authorization: Bearer <token>
```

**Query Parameters:**
- `startDate` - Filter transactions from this date (optional)
- `endDate` - Filter transactions until this date (optional)

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "category": "Food",
      "totalAmount": 150.75,
      "transactionCount": 12,
      "debitsCount": 12,
      "creditsCount": 0
    },
    {
      "category": "Transfer",
      "totalAmount": 500.00,
      "transactionCount": 2,
      "debitsCount": 1,
      "creditsCount": 1
    }
  ],
  "message": "Category summary retrieved successfully"
}
```

---

## 2. Transaction Export (CSV/PDF)

### Overview
Users can export their transaction history in CSV or PDF format with optional filtering by category and date range.

### Export Service
- **Service Class:** `TransactionExportService`
- **Location:** `/Services/TransactionExportService.cs`

#### Features
- Export to CSV format using CsvHelper library
- Export to PDF format using iTextSharp library
- Include transaction summary (totals by type)
- Filter by category and date range
- Professional PDF formatting with headers and tables

### API Endpoints

#### Export to CSV
```
GET /api/wallet/transactions/export/csv?category=Food&startDate=2025-12-01&endDate=2025-12-31
Authorization: Bearer <token>
```

**Query Parameters:**
- `category` - Filter by category (optional)
- `startDate` - Start date for filtering (optional, format: YYYY-MM-DD)
- `endDate` - End date for filtering (optional, format: YYYY-MM-DD)

**Response:**
- Content-Type: `text/csv`
- File download: `transactions_20251231_103045.csv`

**CSV Format:**
```
Id,Amount,Type,Date,Description,Category
1,25.50,Debit,2025-12-31 10:30:00,Lunch,Food
2,500.00,Credit,2025-12-31 11:00:00,Salary Deposit,Salary
```

#### Export to PDF
```
GET /api/wallet/transactions/export/pdf?category=Food&startDate=2025-12-01&endDate=2025-12-31
Authorization: Bearer <token>
```

**Query Parameters:**
- `category` - Filter by category (optional)
- `startDate` - Start date for filtering (optional, format: YYYY-MM-DD)
- `endDate` - End date for filtering (optional, format: YYYY-MM-DD)

**Response:**
- Content-Type: `application/pdf`
- File download: `transactions_20251231_103045.pdf`

**PDF Contains:**
- User name and generation timestamp
- Transaction table with columns: ID, Date, Type, Description, Amount, Category
- Transaction summary showing total credits and debits

### Usage Examples

#### Example 1: Export all food expenses as CSV
```bash
curl -X GET "https://api.example.com/api/wallet/transactions/export/csv?category=Food" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -o food_expenses.csv
```

#### Example 2: Export transactions for a specific month as PDF
```bash
curl -X GET "https://api.example.com/api/wallet/transactions/export/pdf?startDate=2025-12-01&endDate=2025-12-31" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -o december_transactions.pdf
```

#### Example 3: Get category summary
```bash
curl -X GET "https://api.example.com/api/wallet/transactions/summary/by-category?startDate=2025-12-01" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Installation & Dependencies

### NuGet Packages Added
```xml
<PackageReference Include="CsvHelper" Version="33.0.1" />
<PackageReference Include="iTextSharp" Version="5.5.13.3" />
```

### Install Dependencies
```bash
dotnet restore
```

---

## Database Migration

The migration `AddTransactionCategory` was applied to add the `Category` column to the `Transactions` table:

```sql
ALTER TABLE "Transactions" ADD "Category" text NOT NULL DEFAULT '';
```

To revert if needed:
```bash
dotnet ef migrations remove
```

---

## Best Practices

1. **Category Selection:** Use predefined categories when creating transactions for consistency
2. **Date Range Filtering:** Use ISO 8601 format (YYYY-MM-DD) for date parameters
3. **Large Exports:** For large datasets, consider using CSV format for faster generation
4. **File Naming:** Exported files include timestamps to avoid naming conflicts

---

## Error Handling

All endpoints return standard `ApiResponse` format:

**Success Response:**
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation successful"
}
```

**Error Response:**
```json
{
  "success": false,
  "data": null,
  "message": "Error description",
  "errors": ["Detailed error message"]
}
```

---

## Future Enhancements

- [ ] Custom category creation by users
- [ ] Category spending limits and alerts
- [ ] Recurring transaction templates with categories
- [ ] Excel export format
- [ ] Advanced analytics dashboard
- [ ] Category-based budgeting features
