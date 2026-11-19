
# DocuVault

**Document Management & File Integrity Platform**

DocuVault is a document management and file integrity verification platform built with C# and ASP.NET Core.

It allows users to upload documents, securely store file metadata, calculate SHA-256 hashes, and verify whether files have been modified since they were uploaded.

The project demonstrates practical backend engineering skills, including REST API design, file processing, database persistence, authentication, automated testing, and containerized deployment.

## Features

### Planned Features

- [ ] Document upload and metadata management
- [ ] SHA-256 file hashing
- [ ] Document integrity verification
- [ ] Document listing and retrieval
- [ ] User authentication and authorization
- [ ] Document ownership and access control
- [ ] PostgreSQL persistence
- [ ] Docker-based deployment
- [ ] Automated unit and integration tests
- [ ] API documentation with OpenAPI

## Technology Stack

| Category | Technology |
| --- | --- |
| Language | C# |
| Framework | .NET 10 |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Database | PostgreSQL |
| Authentication | ASP.NET Core Identity / JWT |
| Validation | FluentValidation |
| Testing | xUnit |
| Integration Testing | Testcontainers |
| API Documentation | OpenAPI |
| Containerization | Docker / Docker Compose |
| CI/CD | GitHub Actions |

## Architecture

DocuVault follows a simplified Clean Architecture approach to separate business logic, application workflows, infrastructure concerns, and API delivery.

```text
DocuVault/
├── src/
│   ├── DocuVault.Api/
│   ├── DocuVault.Application/
│   ├── DocuVault.Domain/
│   └── DocuVault.Infrastructure/
│
├── tests/
│   ├── DocuVault.UnitTests/
│   └── DocuVault.IntegrationTests/
│
├── docs/
│   ├── architecture.md
│   └── database-design.md
│
├── .github/
│   └── workflows/
│
├── .editorconfig
├── .gitignore
├── mise.toml
├── README.md
└── DocuVault.slnx
```

### Project Dependencies

- `DocuVault.Api` → Application, Infrastructure
- `DocuVault.Application` → Domain
- `DocuVault.Infrastructure` → Application, Domain
- `DocuVault.Domain` → No external project dependencies

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker
- Docker Compose
- Git

### Clone the Repository

```bash
git clone https://github.com/reginaldchiu/docuvault.git
cd docuvault-dotnet
```

### Restore Dependencies

```bash
dotnet restore
```

### Build the Solution

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

> Database configuration and detailed setup instructions will be added as the project develops.

## API Documentation

API documentation will be available through OpenAPI when the application is running.

## Development Roadmap

- [x] Project environment preparation
- [ ] Repository and solution initialization
- [ ] Clean Architecture project structure
- [ ] Domain model and document metadata
- [ ] SHA-256 file hashing service
- [ ] Document upload API
- [ ] Document integrity verification API
- [ ] PostgreSQL integration
- [ ] Authentication and authorization
- [ ] Unit and integration testing
- [ ] Docker deployment
- [ ] CI/CD pipeline
- [ ] Production-ready documentation

## Testing Strategy

The project uses automated tests to verify business logic and API behavior.

- Unit tests for domain rules and application services
- Integration tests for API endpoints and database operations
- File hashing tests for correctness and edge cases
- Validation tests for invalid requests and unsupported files

## Security Considerations

Security is an important part of the project.

Planned security measures include:

- File size and file type validation
- User authentication and authorization
- Document ownership enforcement
- Safe file storage practices
- Protection against path traversal
- Secure handling of configuration and secrets

## License

This project is currently under development.

License information will be added before the first public release.
