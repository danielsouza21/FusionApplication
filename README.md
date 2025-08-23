# FusionCache Application

A comprehensive .NET application demonstrating advanced caching patterns using [FusionCache](https://github.com/ZiggyCreatures/FusionCache/tree/main) with Microsoft Aspire for distributed system orchestration.

## 🎯 Project Overview

This project showcases FusionCache, an easy-to-use, fast, and robust hybrid cache with advanced resiliency features. FusionCache is a production-ready caching solution that has been downloaded over 15 million times and is used by Microsoft in products like Data API Builder.

## 🏗️ Infrastructure

### **Microsoft Aspire Integration**
- **Distributed Application Host**: Orchestrates multiple services with automatic service discovery
- **PostgreSQL Database**: Persistent data storage with pgAdmin for database management
- **Redis Cache**: Distributed caching layer with backplane support
- **Load Balancing**: Automatic load balancing across multiple application instances
- **Health Monitoring**: Built-in health checks and monitoring dashboard

### **Architecture Components**
- **3 Application Replicas**: Load-balanced instances for high availability
- **Persistent Storage**: Database and cache data survive container restarts
- **Service Mesh**: Automatic inter-service communication and discovery
- **Development Dashboard**: Real-time monitoring of all services

## 📋 Features to Study

The following FusionCache features will be implemented and studied:

- [ ] **Eager Refresh**: Automatically refresh cache entries before they expire
- [ ] **Backplane**: Distributed cache synchronization across multiple instances
- [ ] **Soft/Hard Timeout**: Configurable timeouts for factory operations
- [ ] **Fail-Safe**: Use expired cache entries when the data source is unavailable
- [ ] **Cache Stampede**: Prevent multiple simultaneous requests for the same data
- [ ] **Conditional Refresh**: Refresh cache entries based on specific conditions

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code

### Running the Application

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd FusionCacheApplication
   ```

2. **Set FusionCacheApplication.AppHost as startup project**

3. **Run the application**
   ```bash
   dotnet run --project FusionCacheApplication.AppHost
   ```

4. **Access the services**
   - **Aspire Dashboard**: https://localhost:7000
   - **Application API**: https://localhost:7001 (load balanced)
   - **Swagger UI**: https://localhost:7001/swagger
   - **pgAdmin**: https://localhost:7002

## 🔌 API Endpoints

### User Management
- `GET /users/{id}` - Retrieve user by ID
- `POST /users` - Create or update user
- `DELETE /users/{id}` - Delete user by ID

### Admin Operations
- `GET /admin/db/chaos` - Get chaos settings
- `POST /admin/db/fail` - Toggle database failure simulation
- `POST /admin/db/slow` - Set database response delay

### Health & Monitoring
- `GET /health` - Application health check
- `GET /` - Redirects to Swagger UI

## 🏛️ Architecture

### **Domain Layer**
- `User` - Core domain entity with business logic
- `ChaosSettings` - Chaos engineering configuration
- `IUserRepository` - Repository interface for data access

### **Application Layer**
- `UserService` - Application service with business operations
- `IUserService` - Service interface

### **Infrastructure Layer**
- `AppDbContext` - Entity Framework context
- `EfUserRepository` - PostgreSQL data access implementation
- `DatabaseMigrationService` - Database schema management
- `FusionCacheService` - Caching service configuration

### **Configuration**
- `ConfigurationExtensions` - Dependency injection setup
- `FusionCacheConfiguration` - Cache configuration model
- `ConfigurationConstants` - Configuration constants

## ⚙️ Configuration

### **FusionCache Settings**
```json
{
  "FusionCache": {
    "UseBackplaneDistributed": false
  }
}
```

### **Connection Strings**
- **Database**: `fusionApplicationDb` (PostgreSQL)
- **Redis**: `redisBackplaneFusion` (Cache backplane)

## 🧪 Testing Features

### **Cache Testing**
- Test cache hit/miss scenarios
- Verify distributed cache synchronization
- Monitor cache performance metrics

### **Chaos Engineering**
- Simulate database failures
- Test fail-safe mechanisms
- Measure timeout handling

### **Load Testing**
- Multiple application instances
- Load balancing verification
- Performance under stress

## 📊 Monitoring

### **Aspire Dashboard**
- Real-time service status
- Resource utilization
- Log aggregation
- Performance metrics

### **Health Checks**
- Database connectivity
- Cache availability
- Application readiness

## 📚 References

- **[FusionCache GitHub Repository](https://github.com/ZiggyCreatures/FusionCache/tree/main)** - Official FusionCache documentation and source code
- **[Microsoft Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)** - Aspire framework documentation
- **[FusionCache NuGet Package](https://www.nuget.org/packages/ZiggyCreatures.FusionCache)** - Official NuGet package

---

**Note**: This project is designed for learning and demonstrating advanced caching patterns. FusionCache is production-ready and has been used in real-world applications handling billions of requests.
