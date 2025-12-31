# DotNetWeb API

A complete ASP.NET Core RESTful API with **User Authentication** and **Wallet Management** functionality.

## 🚀 Features

### Authentication
- ✅ User Registration with automatic wallet creation
- ✅ User Login with JWT token generation
- ✅ Token Refresh endpoint
- ✅ Secure password hashing with ASP.NET Identity

### User Management
- ✅ Get user profile
- ✅ Update user profile
- ✅ Change password
- ✅ Delete account

### Wallet Operations
- ✅ Get wallet balance
- ✅ Deposit funds
- ✅ Withdraw funds
- ✅ Transfer funds to other users
- ✅ Transaction history with pagination
- ✅ Get transaction by ID

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 10.0
- **Database**: SQLite (with Entity Framework Core)
- **Authentication**: JWT Bearer Tokens
- **Identity**: ASP.NET Core Identity
- **API Documentation**: Swagger/OpenAPI

## 📦 Getting Started

### Prerequisites
- .NET 10.0 SDK

### Installation

1. Clone the repository
2. Navigate to the project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```

4. Apply database migrations:
   ```bash
   dotnet tool run dotnet-ef database update
   ```

5. Run the application:
   ```bash
   dotnet run
   ```

6. Open Swagger UI at: `https://localhost:5001/swagger` or `http://localhost:5000/swagger`

## 📚 API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Login and get JWT token |
| POST | `/api/auth/refresh-token` | Refresh JWT token (requires auth) |

### User Management (Requires Authentication)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/user/profile` | Get current user profile |
| PUT | `/api/user/profile` | Update user profile |
| POST | `/api/user/change-password` | Change password |
| DELETE | `/api/user/account` | Delete user account |

### Wallet Operations (Requires Authentication)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/wallet/balance` | Get wallet balance |
| POST | `/api/wallet/deposit` | Deposit funds |
| POST | `/api/wallet/withdraw` | Withdraw funds |
| POST | `/api/wallet/transfer` | Transfer funds to another user |
| GET | `/api/wallet/transactions` | Get transaction history (paginated) |
| GET | `/api/wallet/transactions/{id}` | Get specific transaction |

## 📝 API Request/Response Examples

### Register User

**Request:**
```http
POST /api/auth/register
Content-Type: application/json

{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "userId": "abc123...",
    "email": "john@example.com"
  }
}
```

### Login

**Request:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiration": "2025-01-07T12:00:00Z",
    "user": {
      "id": "abc123...",
      "email": "john@example.com",
      "fullName": "John Doe"
    }
  }
}
```

### Deposit

**Request:**
```http
POST /api/wallet/deposit
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 100.00
}
```

**Response:**
```json
{
  "success": true,
  "message": "Deposit successful",
  "data": {
    "newBalance": 100.00,
    "transactionId": 1
  }
}
```

### Transfer

**Request:**
```http
POST /api/wallet/transfer
Authorization: Bearer {token}
Content-Type: application/json

{
  "receiverEmail": "jane@example.com",
  "amount": 25.00
}
```

**Response:**
```json
{
  "success": true,
  "message": "Transfer successful",
  "data": {
    "newBalance": 75.00,
    "transactionId": 2,
    "receiverEmail": "jane@example.com",
    "amount": 25.00
  }
}
```

### Get Transactions (Paginated)

**Request:**
```http
GET /api/wallet/transactions?page=1&pageSize=10
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Transactions retrieved successfully",
  "data": {
    "transactions": [
      {
        "id": 2,
        "amount": 25.00,
        "type": "Debit",
        "date": "2025-12-31T12:30:00Z",
        "description": "Transfer to jane@example.com"
      },
      {
        "id": 1,
        "amount": 100.00,
        "type": "Credit",
        "date": "2025-12-31T12:00:00Z",
        "description": "Deposit"
      }
    ],
    "totalCount": 2,
    "page": 1,
    "pageSize": 10,
    "totalPages": 1
  }
}
```

## 🔐 Authentication

All protected endpoints require a JWT token in the Authorization header:

```
Authorization: Bearer your-jwt-token-here
```

## 📁 Project Structure

```
dotnetweb/
├── Controllers/
│   ├── AuthController.cs      # Authentication endpoints
│   ├── UserController.cs      # User management endpoints
│   └── WalletController.cs    # Wallet operations endpoints
├── Data/
│   └── ApplicationDbContext.cs # EF Core database context
├── DTOs/
│   ├── ApiResponse.cs         # Generic API response wrapper
│   ├── ChangePasswordDto.cs   # Change password request
│   ├── DepositDto.cs          # Deposit request
│   ├── LoginDto.cs            # Login request
│   ├── RegisterDto.cs         # Registration request
│   ├── TransactionDto.cs      # Transaction response
│   ├── TransferDto.cs         # Transfer request
│   ├── UpdateProfileDto.cs    # Update profile request
│   ├── UserProfileDto.cs      # User profile response
│   └── WithdrawDto.cs         # Withdraw request
├── Migrations/                 # EF Core migrations
├── Models/
│   ├── Transaction.cs         # Transaction entity
│   ├── User.cs                # User entity (extends IdentityUser)
│   └── Wallet.cs              # Wallet entity
├── Program.cs                  # Application entry point
├── appsettings.json           # Configuration
└── app.db                     # SQLite database file
```

## ⚙️ Configuration

The `appsettings.json` file contains the following configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "http://localhost:5248",
    "Audience": "http://localhost:5248"
  }
}
```

> ⚠️ **Important**: In production, replace the JWT key with a strong, unique secret and store it securely (e.g., environment variables, Azure Key Vault).

## 📄 License

MIT License
