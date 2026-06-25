# Developer Evaluation Project - Sales API

This project is a prototype implementation of the **Sales API** for the DeveloperStore, built upon a Clean Architecture template utilizing .NET 8. The solution is designed following Domain-Driven Design (DDD) principles, CQRS (Command Query Responsibility Segregation), and Domain Event Auditing.

---

## 1. Clean Architecture & Structure

The repository is structured into the following layers to separate concerns and ensure maintainability:

*   **Domain (`Ambev.DeveloperEvaluation.Domain`)**:
    Contains the core business model. Includes rich entities (`Sale`, `SaleItem`), domain validators (`SaleValidator`, `SaleItemValidator`), repositories interfaces, and domain events (`SaleCreatedEvent`, `SaleModifiedEvent`, `SaleCancelledEvent`, `ItemCancelledEvent`).
*   **Application (`Ambev.DeveloperEvaluation.Application`)**:
    Implements business use cases using the CQRS pattern via MediatR handlers. This layer defines commands, queries, validators, AutoMapper profiles, and abstractions such as the `IEventPublisher` interface.
*   **Infrastructure/ORM (`Ambev.DeveloperEvaluation.ORM`)**:
    Deals with data access and concrete implementations of abstractions. It encapsulates the EF Core PostgreSQL DbContext (`DefaultContext`), entity configurations, database migrations, concrete repository implementations (`SaleRepository`), and the MongoDB audit logging implementation (`LoggingEventPublisher`).
*   **IoC (`Ambev.DeveloperEvaluation.IoC`)**:
    Bootstraps dependency injection registrations, grouping registrations by modules (e.g., `InfrastructureModuleInitializer`, `ApplicationModuleInitializer`).
*   **WebApi (`Ambev.DeveloperEvaluation.WebApi`)**:
    The presentation layer. Exposes REST API endpoints via ASP.NET Core controllers (`SalesController`), maps incoming HTTP requests to CQRS commands/queries, and returns standardized JSON responses.

---

## 2. Technical Decisions & Patterns

### Rich Domain Model (DDD)
Business rules (like discount tiers and maximum item limits) are encapsulated directly inside the domain entities (`Sale` and `SaleItem`). This ensures that the domain remains rich and prevents business logic from leaking into handlers or controllers.

### External Identities & Denormalization
To refer to entities outside the Sales boundary (such as Customers, Branches, and Products) while respecting domain boundaries:
*   We reference external resources by their ID (`CustomerId`, `BranchId`, `ProductId`).
*   We denormalize and persist historical descriptions (`CustomerName`, `BranchName`, `ProductName`) at the moment of sale creation. This guarantees historical consistency even if a customer or product name changes in their respective systems in the future.

### Event Auditing with MongoDB
*   An `IEventPublisher` abstraction is defined in the **Application** layer.
*   The handlers trigger domain events (e.g., `SaleCreatedEvent`, `SaleModifiedEvent`, `SaleCancelledEvent`, `ItemCancelledEvent`) via this abstraction.
*   The concrete implementation (`LoggingEventPublisher`) resides in the **Infrastructure** layer. It logs events using `ILogger` and stores them in a MongoDB audit collection named `SalesEventsAudit` within the `developer_evaluation_audit` database.
*   PostgreSQL continues to be the primary transactional source of truth for Sales and Sale Items, maintaining database ACID constraints.

### Reconciliation Logic in Updates
The `UpdateSale` use case performs automatic reconciliation of items:
*   **Adding items**: New products in the update request are appended to the sale.
*   **Updating items**: Existing products have their quantities and unit prices updated, and discounts are recalculated.
*   **Removing items**: Products omitted from the update request are removed from the sale.
*   **Reactivation Rule**: An item that has been marked as cancelled (`IsCancelled = true`) cannot be reactivated. Any update request attempting to flip a cancelled item's `IsCancelled` flag back to `false` triggers a validation/domain exception.

---

## 3. Business Rules

### Quantity-Based Discount Tiers
Every time an item is added or updated, discount calculations are executed inside the domain:
*   **Quantities < 4 items**: No discount (0%).
*   **Quantities between 4 and 9 items (inclusive)**: 10% discount on the unit price.
*   **Quantities between 10 and 20 items (inclusive)**: 20% discount on the unit price.
*   **Quantities > 20 items**: Invalid. The system throws an exception ("It's not possible to sell above 20 identical items").

### Cancellation & Total Calculation
*   Sales and individual Sale Items support cancellation via the `IsCancelled` boolean property.
*   Total calculations only include items where `IsCancelled` is `false`.
*   Cancelling a Sale cascades cancellation to all of its items.

---

## 4. REST API Endpoints

All sales endpoints are prefixed with `/api/sales`:

### Create Sale
*   **Method**: `POST`
*   **Route**: `/api/sales`
*   **Payload Example**:
    ```json
    {
      "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "customerName": "John Doe",
      "branchId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "branchName": "Main Branch",
      "items": [
        {
          "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
          "productName": "Sleek Phone",
          "quantity": 5,
          "unitPrice": 100.00
        }
      ]
    }
    ```
*   **Response**: `201 Created` with created Sale details (including generated `SaleNumber`, calculated `Discount` [10%], and `TotalAmount` [450.00]).

### Get Sale
*   **Method**: `GET`
*   **Route**: `/api/sales/{id}`
*   **Response**: `200 OK` with Sale record details.

### Update Sale
*   **Method**: `PUT`
*   **Route**: `/api/sales/{id}`
*   **Payload Example**:
    ```json
    {
      "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "customerName": "John Doe",
      "branchId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "branchName": "Main Branch",
      "items": [
        {
          "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
          "productName": "Sleek Phone",
          "quantity": 10,
          "unitPrice": 95.00,
          "isCancelled": false
        }
      ]
    }
    ```
*   **Response**: `200 OK` with updated details (quantity updated to 10, discount recalculated to 20%, and `TotalAmount` updated to 760.00).

### List Sales
*   **Method**: `GET`
*   **Route**: `/api/sales`
*   **Query Parameters (Optional)**:
    *   `_page`: page number (default: 1)
    *   `_size`: items per page (default: 10)
    *   `_order`: sorting order (e.g., `SaleDate desc`, `TotalAmount asc`)
    *   `customerId`: filter by Customer ID (`Guid`)
    *   `branchId`: filter by Branch ID (`Guid`)
    *   `minDate`: filter by minimum Sale date (`DateTime`)
    *   `maxDate`: filter by maximum Sale date (`DateTime`)
*   **Response**: `200 OK` with paginated lists and metadata.

### Cancel Sale
*   **Method**: `PUT`
*   **Route**: `/api/sales/{id}/cancel`
*   **Response**: `200 OK` with the cancelled sale confirmation.

---

## 5. Local Execution Guide

### Prerequisite Setup
Make sure you have:
1.  **.NET 8 SDK** installed locally.
2.  **Docker Desktop** running.

### Exposing Docker Databases
Run the Docker Compose environment to spin up PostgreSQL, MongoDB, and Redis:
```bash
docker compose up -d
```
*   **PostgreSQL**: Maps to `localhost:5432` (Username: `developer`, Password: `ev@luAt10n`, Database: `developer_evaluation`).
*   **MongoDB**: Maps to `localhost:27017` (Username: `developer`, Password: `ev@luAt10n`, Database: `developer_evaluation_audit`).

### Applying PostgreSQL Migrations
Apply the EF migrations to configure the PostgreSQL database tables:
```bash
dotnet ef database update --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi
```

### Running the Web API
Start the WebApi server:
```bash
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.csproj
```
The Swagger interactive documentation page will be available at:
`http://localhost:8080/swagger` (or `https://localhost:8081/swagger`)

---

## 6. Testing

The solution includes automated unit testing covering the domain rules and CQRS handler workflows.

### Running Tests
Execute the tests using the .NET CLI:
```bash
dotnet test
```

### Test Coverage Highlights
*   **Domain Validation**:
    *   Validates discount thresholds (no discount on quantities < 4, 10% on 4-9, 20% on 10-20).
    *   Rejects requests with item quantities exceeding 20.
    *   Rejects reactivation of cancelled items.
*   **CQRS Use Cases**:
    *   `CreateSaleHandler` generates correct sale numbers and triggers creation domain events.
    *   `UpdateSaleHandler` correctly reconciles items (add, update, delete) and publishes updates.
    *   `CancelSaleHandler` cancels all elements and generates individual cancellation audits.
    *   `ListSalesHandler` properly maps pagination, sorting, and optional parameters.
