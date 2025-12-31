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
- **Database**: PostgreSQL (with Entity Framework Core & Npgsql)
- **Authentication**: JWT Bearer Tokens
- **Identity**: ASP.NET Core Identity
- **API Documentation**: Swagger/OpenAPI
- **Containerization**: Docker & Docker Compose

## 📦 Getting Started

### Prerequisites
- .NET 10.0 SDK
- Docker & Docker Compose (for PostgreSQL)

### Installation & Setup

#### 1. Clone the repository and navigate to the project directory
```bash
cd dotnetweb
```

#### 2. Restore dependencies
```bash
dotnet restore
dotnet tool restore
```

#### 3. Start PostgreSQL using Docker Compose
```bash
docker compose -f docker-compose.yml up -d --build
```
This starts a PostgreSQL container with credentials from `docker-compose.yml`:
- **User**: dotnetuser
- **Password**: dotnetpass123
- **Database**: dotnetwebdb
- **Port**: 5432

#### 4. Apply Entity Framework migrations
Run pending migrations to initialize the database schema:
```bash
dotnet ef database update
```

**Alternative: Create a new migration** (if schema changes):
```bash
dotnet ef migrations add MigrationName --output-dir Migrations
dotnet ef database update
```

#### 5. Run the application
```bash
dotnet run
```

The API starts on: `http://localhost:5113`

#### 6. Access Swagger UI
Open your browser to: `http://localhost:5113/swagger`

### Environment Variables

The application uses `.env` file for configuration:

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_USER` | PostgreSQL username | dotnetuser |
| `POSTGRES_PASSWORD` | PostgreSQL password | dotnetpass123 |
| `POSTGRES_DB` | PostgreSQL database name | dotnetwebdb |
| `POSTGRES_PORT` | PostgreSQL port | 5432 |
| `POSTGRES_HOST` | PostgreSQL host | localhost |
| `JWT_KEY` | JWT signing key | (see .env.example) |
| `JWT_ISSUER` | JWT issuer | http://localhost:5113 |
| `JWT_AUDIENCE` | JWT audience | http://localhost:5113 |
| `DB_CONNECTION_STRING` | Full connection string | (auto-constructed if not set) |
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | Development |

### Useful EF Core CLI Commands

| Command | Description |
|---------|-------------|
| `dotnet ef migrations list` | List all migrations |
| `dotnet ef migrations add {name}` | Create a new migration |
| `dotnet ef database update` | Apply pending migrations |
| `dotnet ef database update {migration-name}` | Revert/move to specific migration |
| `dotnet ef migrations remove` | Remove the last migration |
| `dotnet ef dbcontext info` | Display DbContext info |

### Stopping PostgreSQL
```bash
docker compose -f docker-compose.yml down
```

### Stopping PostgreSQL and removing data
```bash
docker compose -f docker-compose.yml down -v
```

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
│   ├── AuthController.cs       # Authentication endpoints
│   ├── UserController.cs       # User management endpoints
│   └── WalletController.cs     # Wallet operations endpoints
├── Data/
│   └── ApplicationDbContext.cs # EF Core database context
├── DTOs/
│   ├── ApiResponse.cs          # Generic API response wrapper
│   ├── ChangePasswordDto.cs    # Change password request
│   ├── DepositDto.cs           # Deposit request
│   ├── LoginDto.cs             # Login request
│   ├── RegisterDto.cs          # Registration request
│   ├── TransactionDto.cs       # Transaction response
│   ├── TransferDto.cs          # Transfer request
│   ├── UpdateProfileDto.cs     # Update profile request
│   ├── UserProfileDto.cs       # User profile response
│   └── WithdrawDto.cs          # Withdraw request
├── Migrations/                  # EF Core migrations (PostgreSQL)
│   ├── 20251231124841_InitialCreate.cs
│   ├── 20251231124841_InitialCreate.Designer.cs
│   ├── 20251231134305_InitialCreatePg.cs
│   ├── 20251231134305_InitialCreatePg.Designer.cs
│   └── ApplicationDbContextModelSnapshot.cs
├── Models/
│   ├── Transaction.cs          # Transaction entity
│   ├── User.cs                 # User entity (extends IdentityUser)
│   └── Wallet.cs               # Wallet entity
├── Properties/
│   └── launchSettings.json     # Launch profile settings
├── Program.cs                   # Application entry point & service registration
├── appsettings.json            # Configuration (connection string, JWT)
├── appsettings.Development.json# Development-specific configuration
├── docker-compose.yml          # PostgreSQL container setup
├── dotnet-tools.json           # Local tool references (dotnet-ef)
├── dotnetweb.csproj            # Project file
└── README.md                   # This file
```

## ⚙️ Configuration

### Database Connection

The `appsettings.json` file contains the PostgreSQL connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dotnetwebdb;Username=dotnetuser;Password=dotnetpass123"
  },
  "Jwt": {
    "Key": "ThisIsASecretKeyForMyAwesomeAppThatIsLongEnoughToSecureIt12345!",
    "Issuer": "http://localhost:5248",
    "Audience": "http://localhost:5248"
  }
}
```

> ⚠️ **Important**: In production:
> - Replace the JWT key with a strong, unique secret and store it securely (e.g., environment variables, Azure Key Vault)
> - Use a secure PostgreSQL password (currently set to `dotnetpass123` for development)
> - Update connection string to match production database

## 📄 License

MIT License
