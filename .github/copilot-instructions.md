# Copilot Instructions for Cosei

## Repository Overview

Cosei is a .NET library that enables calling ASP.NET Core Controllers from other sources, providing flexibility in how you expose and consume controller endpoints beyond traditional HTTP requests.

## Purpose and Functionality

The library provides:
- **Client Libraries**: Generate clients to call controllers from various transport mechanisms
- **Service Libraries**: Enable controllers to be called through different transport layers
- **Transport Options**: Currently supports HTTP and RabbitMQ message queues
- **ASP.NET Core Integration**: Seamless integration with existing ASP.NET Core applications

## Architecture and Project Structure

The solution follows a clear separation of concerns with these core components:

### Base Libraries
- **Cosei.Client.Base**: Core client abstractions and base functionality
- **Cosei.Service.Base**: Core service abstractions and base functionality

### Transport Implementations
- **Cosei.Client.Http**: HTTP transport implementation for clients
- **Cosei.Client.RabbitMq**: RabbitMQ transport implementation for clients  
- **Cosei.Service.Http**: HTTP transport implementation for services
- **Cosei.Service.RabbitMq**: RabbitMQ transport implementation for services

### Examples
- **Cosei.Examples.DemoClient**: Example client implementation
- **Cosei.Examples.DemoService**: Example service implementation
- **Cosei.Examples.DemoContracts**: Shared contracts for examples

## Technology Stack

- **.NET Version**: 9.0 (latest)
- **Language**: C# with latest language features enabled
- **Frameworks**: ASP.NET Core, Microsoft.Extensions.Hosting
- **Messaging**: RabbitMQ for message queue transport
- **Serialization**: System.Text.Json
- **Logging**: Microsoft.Extensions.Logging

## Development Guidelines

### Coding Standards

1. **Language Version**: Use `<LangVersion>latest</LangVersion>` to leverage newest C# features
2. **Nullable Reference Types**: Enable nullable reference types for better null safety
3. **Async/Await**: Use async patterns consistently for I/O operations
4. **Dependency Injection**: Leverage Microsoft.Extensions.DependencyInjection patterns
5. **Configuration**: Use Microsoft.Extensions.Configuration for settings

### File Organization

- Place interface definitions in the root of each project
- Group related functionality into logical folders
- Keep transport-specific implementations in separate projects
- Maintain clear separation between client and service concerns

### Naming Conventions

- **Interfaces**: Prefix with `I` (e.g., `IPublisher`, `IRequestDelegateProvider`)
- **Implementations**: Use descriptive names without `Impl` suffix
- **Extensions**: Use `Extensions` suffix for extension classes
- **Configurations**: Use `Configuration` suffix for config classes

### Dependencies and Package Management

- **Base Projects**: Minimal dependencies, focus on abstractions
- **Implementation Projects**: Add transport-specific dependencies as needed
- **Version Management**: Keep package versions consistent across projects
- **Framework References**: Use `<FrameworkReference Include="Microsoft.AspNetCore.App" />` for ASP.NET Core

### Project Configuration Standards

Each project should include:
```xml
<PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Authors>Najlot</Authors>
    <Product>Cosei</Product>
    <Description>Call ASP.NET Core Controllers from other sources</Description>
    <PackageProjectUrl>https://github.com/najlot/Cosei</PackageProjectUrl>
    <RepositoryUrl>https://github.com/najlot/Cosei</RepositoryUrl>
    <PackageTags>cosei asp.net</PackageTags>
    <GeneratePackageOnBuild>True</GeneratePackageOnBuild>
    <PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>
    <PackageLicenseFile>LICENSE</PackageLicenseFile>
    <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>
```

## Build and Development

### Building the Solution
```bash
cd src
dotnet build Cosei.sln
```

### Development Environment
- Requires .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension
- For RabbitMQ development: Docker or local RabbitMQ installation

### Package Generation
- Projects are configured to generate NuGet packages on build
- Version is controlled via `<Version>` property in project files
- All projects share common metadata for consistency

## Key Patterns and Principles

1. **Publisher Pattern**: Use `IPublisher` for sending messages/requests
2. **Request Delegate Pattern**: Controllers are wrapped as request delegates
3. **Transport Abstraction**: Transport mechanisms are abstracted behind common interfaces
4. **Extension Methods**: Use extension methods for service registration and configuration
5. **Factory Pattern**: Use factory patterns for creating transport-specific implementations

## Making Changes

When adding new features:

1. **New Transport**: Create both client and service implementations
2. **New Functionality**: Add to base libraries first, then implement in transports
3. **Breaking Changes**: Update version numbers appropriately
4. **Configuration**: Add new configuration options to relevant Configuration classes
5. **Examples**: Update examples to demonstrate new features

## Common Tasks

### Adding a New Transport
1. Create `Cosei.Client.{Transport}` project
2. Create `Cosei.Service.{Transport}` project  
3. Implement required interfaces from base libraries
4. Add configuration classes
5. Provide service collection extensions
6. Add example usage

### Extending Functionality
1. Add abstractions to base libraries
2. Implement in existing transport projects
3. Update configuration if needed
4. Add examples demonstrating usage

## Testing and Quality

While the repository currently doesn't have formal tests, when adding them:
- Use xUnit for testing framework
- Create separate test projects for each main project
- Focus on integration tests for transport implementations
- Mock external dependencies (RabbitMQ, HTTP clients)