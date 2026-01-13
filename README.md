# RTCodingExercise.Microservices

A microservices-based number plate catalog and sales platform built with ASP.NET Core and Angular.

## Project Overview

This solution demonstrates a microservices architecture for managing UK number plate inventory with the following capabilities:
- Browse and search number plates with filtering by letters, numbers, and status
- Reserve and unreserve plates
- Sell plates with promotional code support
- Real-time revenue statistics and analytics
- Event-driven architecture using MassTransit and RabbitMQ

## Architecture

### Frontend Applications

#### WebMVC
ASP.NET Core 8.0 MVC application providing a server-side rendered interface with:
- Responsive Bootstrap 5 UI with side-by-side dashboard layout
- UK Number Plate authentic styling with proper spacing
- Revenue statistics with Chart.js doughnut charts
- Razor views with runtime compilation enabled

#### WebAngular
Angular 19 single-page application featuring:
- Standalone components architecture
- TypeScript utilities for plate formatting
- Real-time filtering and pagination
- Comprehensive test coverage (63 tests passing)

### Backend Services

#### Catalog.API
ASP.NET Core 8.0 Web API providing:
- RESTful endpoints for plate management
- Repository pattern with Entity Framework Core
- SQL Server database with Code First migrations
- Revenue statistics calculation
- Integration event publishing via MassTransit

#### Catalog.Domain
Domain entities and value objects:
- `Plate` entity with registration, price, status, and metadata
- Domain-driven design principles

### Building Blocks

#### EventBus (IntegrationEvents)
Message contracts for event-driven communication:
- `PlateReservedIntegrationEvent`
- `PlateUnreservedIntegrationEvent`
- `PlateSoldIntegrationEvent`

Integrated with MassTransit and RabbitMQ for asynchronous messaging.

#### WebHost.Customization
Extension methods for database migration on startup.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js 18+](https://nodejs.org/) (for Angular development)

## Getting Started

### Using Docker Compose

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd RTCodingExercise.Microservices
   ```

2. **Start all services**
   ```bash
   docker-compose up -d --build
   ```

3. **Access the applications**
   - WebMVC: http://localhost:5100
   - WebAngular: http://localhost:4200
   - Catalog.API: http://localhost:5101
   - RabbitMQ Management: http://localhost:15672 (guest/guest)

### Running Locally (Development)

#### Catalog.API
```bash
cd src/Services/Catalog/Catalog.API
dotnet run
```

#### WebMVC
```bash
cd src/Web/WebMVC
dotnet run
```

#### WebAngular
```bash
cd src/Web/WebAngular
npm install
npm start
```

## Database

**SQL Server 2019** running in Docker container:
- Database: `RTCodingExercise.Services.CatalogDb`
- Connection String: `Server=localhost,5433;Database=RTCodingExercise.Services.CatalogDb;User Id=sa;Password=Pass@word`
- Automatically created and seeded on startup via Code First migrations
- Connect via Visual Studio Server Explorer or SQL Server Management Studio using `localhost,5433`

### Database Migrations

To create a new migration:
```bash
cd src/Services/Catalog/Catalog.API
dotnet ef migrations add <MigrationName>
```

## Testing

### Backend Tests (.NET)
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test src/Services/Catalog/Catalog.UnitTests/Catalog.UnitTests.csproj
dotnet test src/Web/WebMVC.UnitTests/WebMVC.UnitTests.csproj
```

**Test Coverage:** 143 passing tests across Catalog.API and WebMVC projects using xUnit and Moq.

### Frontend Tests (Angular)
```bash
cd src/Web/WebAngular
npm test
```

**Test Coverage:** 63 passing tests using Vitest.

## Project Structure

```
RTCodingExercise.Microservices/
├── src/
│   ├── BuildingBlocks/
│   │   ├── EventBus/IntegrationEvents/     # Message contracts
│   │   └── WebHost.Customization/          # Shared extensions
│   ├── Services/
│   │   └── Catalog/
│   │       ├── Catalog.API/                # Web API
│   │       ├── Catalog.Domain/             # Domain entities
│   │       └── Catalog.UnitTests/          # API tests
│   └── Web/
│       ├── WebAngular/                     # Angular SPA
│       ├── WebMVC/                         # ASP.NET Core MVC
│       └── WebMVC.UnitTests/               # MVC tests
├── docker-compose.yml                      # Docker orchestration
└── RTCodingExercise.Microservices.sln     # Solution file
```

## Key Features

### UK Number Plate Formatting
Both frontend applications implement authentic UK number plate formatting with proper spacing:
- **Current format:** AB12 ABC (2 letters, 2 numbers, 3 letters)
- **Prefix format:** A123 BCD (1 letter, 1-3 numbers, 3 letters)
- **Suffix format:** ABC 123D (3 letters, 1-3 numbers, 1 letter)
- **Dateless/Cherished:** AB 1234, A 1, etc.

Implemented via:
- C#: `PlateHelpers.FormatRegistration()` static method
- TypeScript: `formatRegistration()` utility function

### Revenue Statistics
Real-time dashboard showing:
- Total revenue from sold plates
- Number of plates sold
- Revenue breakdown by plate type
- Visual Chart.js doughnut chart (400x400px)

### Responsive Design
- Bootstrap 5 grid system
- Mobile-first approach
- Side-by-side stats and chart layout on desktop
- 3-4 column plate grid on larger screens

## Technologies

### Backend
- ASP.NET Core 8.0
- Entity Framework Core (Code First)
- SQL Server 2019
- MassTransit 8.3.4
- RabbitMQ
- xUnit & Moq (testing)

### Frontend
- Angular 19 (standalone components)
- TypeScript 5.7
- Bootstrap 5
- Chart.js 4.4.1
- Vitest (testing)
- UK Number Plate font (custom typography)

### DevOps
- Docker & Docker Compose
- Multi-stage Dockerfile builds
- nginx (Angular production server)

## API Endpoints

### Catalog API (http://localhost:5101/api)

#### Plates
- `GET /plates` - Get paginated list of plates
  - Query params: `pageNumber`, `pageSize`, `searchText`, `letters`, `numbers`, `status`, `sortBy`
- `GET /plates/{id}` - Get plate by ID
- `POST /plates/{id}/reserve` - Reserve a plate
- `DELETE /plates/{id}/reserve` - Unreserve a plate
- `POST /plates/{id}/sell` - Sell a plate
  - Body: `{ "promoCode": "DISCOUNT" }`
- `POST /plates/calculate-price` - Calculate price with promo code
  - Body: `{ "plateId": 1, "promoCode": "DISCOUNT" }`

#### Statistics
- `GET /plates/statistics/revenue` - Get revenue statistics

## Development Guidelines

### Code Organization
- **Helpers/Utilities:** Reusable formatting and helper functions
  - C#: `src/Web/WebMVC/Helpers/PlateHelpers.cs`
  - TypeScript: `src/Web/WebAngular/src/app/utils/plate-helpers.ts`
- **Repository Pattern:** Data access abstraction in Catalog.API
- **Domain-Driven Design:** Business logic in Catalog.Domain
- **Clean Architecture:** Clear separation of concerns

### Best Practices Implemented
- ✅ Async/await pattern throughout
- ✅ Repository pattern for data access
- ✅ Dependency injection
- ✅ Comprehensive unit testing (206 total tests)
- ✅ Error handling and logging
- ✅ Input validation
- ✅ RESTful API design

### Known Technical Debt
- Hardcoded passwords in `appsettings.json` (should use User Secrets/Azure Key Vault)
- Missing API versioning
- No rate limiting implemented
- Missing cancellation tokens in async methods

## Troubleshooting

### Docker Issues
```bash
# Stop all containers
docker-compose down

# Remove volumes and rebuild
docker-compose down -v
docker-compose up -d --build
```

### Database Connection Issues
Ensure SQL Server container is running:
```bash
docker ps | grep sqldata
```

### Angular Build Issues
```bash
cd src/Web/WebAngular
rm -rf node_modules package-lock.json
npm install
```

## License

This project is for demonstration and educational purposes.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request





