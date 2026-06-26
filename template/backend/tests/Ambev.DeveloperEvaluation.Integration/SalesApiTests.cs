using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

public class SalesApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SalesApiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthenticatedTokenAsync()
    {
        // 1. Create a unique user
        var uniqueEmail = $"user_{Guid.NewGuid():N}@ambev.com.br";
        var createUserRequest = new CreateUserRequest
        {
            Username = $"user_{Guid.NewGuid():N}".Substring(0, 15),
            Password = "Password123!",
            Email = uniqueEmail,
            Phone = "+5511999999999",
            Status = UserStatus.Active,
            Role = UserRole.Admin
        };

        var createUserResponse = await _client.PostAsJsonAsync("/api/users", createUserRequest);
        createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Authenticate the user
        var authRequest = new AuthenticateUserRequest
        {
            Email = uniqueEmail,
            Password = "Password123!"
        };

        var authResponse = await _client.PostAsJsonAsync("/api/auth", authRequest);
        authResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResult = await authResponse.Content.ReadFromJsonAsync<ApiResponseWithData<ApiResponseWithData<AuthenticateUserResponse>>>();
        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.Data.Should().NotBeNull();
        authResult.Data!.Success.Should().BeTrue();
        authResult.Data.Data.Should().NotBeNull();
        authResult.Data.Data!.Token.Should().NotBeNullOrEmpty();

        return authResult.Data.Data.Token;
    }

    [Fact(DisplayName = "Given valid credentials When authenticating Then returns JWT token successfully")]
    public async Task Authenticate_ValidCredentials_ReturnsToken()
    {
        // Act & Assert
        var token = await GetAuthenticatedTokenAsync();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "Given request without authorization When accessing Sales API Then returns 401 Unauthorized")]
    public async Task AccessSales_NoToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/sales");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Given request with invalid token When accessing Sales API Then returns 401 Unauthorized")]
    public async Task AccessSales_InvalidToken_Returns401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sales");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "InvalidTokenString");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Given valid token When performing CRUD lifecycle on Sales API Then succeeds")]
    public async Task Sales_CrudLifecycle_Succeeds()
    {
        // Arrange: Get valid token and configure HTTP Client authorization header
        var token = await GetAuthenticatedTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // 1. CREATE SALE
        var createRequest = new CreateSaleRequest
        {
            CustomerId = customerId,
            CustomerName = "John Doe",
            BranchId = branchId,
            BranchName = "Main Branch",
            Items = new List<CreateSaleItemRequest>
            {
                new CreateSaleItemRequest
                {
                    ProductId = productId,
                    ProductName = "Soda Cans",
                    Quantity = 5,
                    UnitPrice = 10.00m
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/sales", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponseWithData<CreateSaleResponse>>();
        createResult.Should().NotBeNull();
        createResult!.Success.Should().BeTrue();
        createResult.Data.Should().NotBeNull();
        createResult.Data!.Id.Should().NotBeEmpty();
        createResult.Data.TotalAmount.Should().Be(45.00m); // 5 items * $10.00 = $50.00 - 10% discount ($5.00) = $45.00

        var saleId = createResult.Data.Id;

        // 2. GET SALE BY ID
        var getResponse = await _client.GetAsync($"/api/sales/{saleId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponseWithData<GetSaleResponse>>();
        getResult.Should().NotBeNull();
        getResult!.Success.Should().BeTrue();
        getResult.Data.Should().NotBeNull();
        getResult.Data!.Id.Should().Be(saleId);
        getResult.Data.CustomerName.Should().Be("John Doe");

        // 3. UPDATE SALE (Update Quantity to 10 items -> 20% discount)
        var updateRequest = new UpdateSaleRequest
        {
            CustomerId = customerId,
            CustomerName = "John Doe Updated",
            BranchId = branchId,
            BranchName = "Main Branch",
            Items = new List<UpdateSaleItemRequest>
            {
                new UpdateSaleItemRequest
                {
                    ProductId = productId,
                    ProductName = "Soda Cans",
                    Quantity = 10,
                    UnitPrice = 10.00m,
                    IsCancelled = false
                }
            }
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/sales/{saleId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResult = await updateResponse.Content.ReadFromJsonAsync<ApiResponseWithData<UpdateSaleResponse>>();
        updateResult.Should().NotBeNull();
        updateResult!.Success.Should().BeTrue();
        updateResult.Data.Should().NotBeNull();
        updateResult.Data!.CustomerName.Should().Be("John Doe Updated");
        updateResult.Data.TotalAmount.Should().Be(80.00m); // 10 items * $10.00 = $100.00 - 20% discount ($20.00) = $80.00

        // 4. CANCEL SALE
        var cancelResponse = await _client.PutAsync($"/api/sales/{saleId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelResult = await cancelResponse.Content.ReadFromJsonAsync<ApiResponseWithData<CancelSaleResult>>();
        cancelResult.Should().NotBeNull();
        cancelResult!.Success.Should().BeTrue();
        cancelResult.Data.Should().NotBeNull();
        cancelResult.Data!.Id.Should().Be(saleId);
        cancelResult.Data.IsCancelled.Should().BeTrue();
    }
}
