<!-- LOGO -->
<p align="center">
  <img src="docs/logo.png" alt="Mevora Logo" width="150"/>
</p>

# Mevora
Mevora enables you to quickly and easily perform certain request/response and event operations using the CQRS and Mediator design patterns. You can review the [documentation](#hakkında) to learn how to use Mevora.

## How does Mevora work?

Mevora identifies all your Request Processor, Pipeline Action, and Message Processor classes at the beginning of the project and creates a main MevoraDispatcher class. It creates the necessary methods for each Processor class within the class and makes them available for your use. Since these operations are performed at compile-time, it offers high-performance usage.

---

## Installation
You can download the Mevora library via Nuget.

```bash
Install-Package Mevora
```
or you can download it using the .NET CLI
```bash
dotnet add package Mevora
```
