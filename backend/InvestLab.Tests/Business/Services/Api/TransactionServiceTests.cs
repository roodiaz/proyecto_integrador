using InvestLab.Business.Services.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Transaction;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static InvestLab.Models.Enums;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="TransactionService"/>, cubriendo el método invocado desde <c>PortfolioController</c>
/// para consultar el historial de transacciones de un portfolio del usuario.
/// </summary>
public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IUserPortfolioRepository> _userPortfolioRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<TransactionService>> _logger = new();

    private TransactionService CreateService() => new(_transactionRepository.Object, _userPortfolioRepository.Object, _unitOfWork.Object, _logger.Object);

    private static UserPortfolio PortfolioEntity(int id = 1, int userId = 1) =>
        new() { Id = id, UserId = userId, Name = "Mi Portfolio", InitialBalance = 10000, CurrentBalance = 10000, IsActive = true };

    private static TransactionDto TransactionEntity(string symbol = "AAPL", TransactionType type = TransactionType.Buy) =>
        new() { OperationDate = DateTime.UtcNow, Symbol = symbol, Type = type, Quantity = 5, BuyPrice = 150, Total = 750 };

    // ---------- GetTransactionHistoryAsync ----------

    /// <summary>Verifica que, si el portfolio no existe o no pertenece al usuario, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetTransactionHistoryAsync_WhenPortfolioDoesNotExist_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync((UserPortfolio?)null);

        var result = await CreateService().GetTransactionHistoryAsync(1, 1, new TransactionFilterDto());

        Assert.False(result.Success);
        Assert.Equal("Portfolio no encontrado", result.Message);
    }

    /// <summary>Verifica que, cuando el portfolio tiene transacciones, se devuelva una respuesta exitosa con los datos y el total de registros encontrados.</summary>
    [Fact]
    public async Task GetTransactionHistoryAsync_WhenPortfolioHasTransactions_ShouldReturnSuccessResponseWithDataAndTotal()
    {
        var transactions = new List<TransactionDto> { TransactionEntity("AAPL"), TransactionEntity("MSFT", TransactionType.Sell) };
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _transactionRepository.Setup(r => r.SearchAsync(1, It.IsAny<TransactionFilterDto>())).ReturnsAsync((transactions, 2));

        var result = await CreateService().GetTransactionHistoryAsync(1, 1, new TransactionFilterDto());

        Assert.True(result.Success);
        var data = Assert.IsType<TransactionSearchResponseDto>(result.Data);
        Assert.Equal(2, data.Total);
        Assert.Equal(transactions, data.Data);
    }

    /// <summary>Verifica que, cuando el portfolio no tiene transacciones, se devuelva una respuesta exitosa con una colección vacía y total cero.</summary>
    [Fact]
    public async Task GetTransactionHistoryAsync_WhenPortfolioHasNoTransactions_ShouldReturnSuccessResponseWithEmptyData()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _transactionRepository.Setup(r => r.SearchAsync(1, It.IsAny<TransactionFilterDto>())).ReturnsAsync((new List<TransactionDto>(), 0));

        var result = await CreateService().GetTransactionHistoryAsync(1, 1, new TransactionFilterDto());

        Assert.True(result.Success);
        var data = Assert.IsType<TransactionSearchResponseDto>(result.Data);
        Assert.Empty(data.Data);
        Assert.Equal(0, data.Total);
    }

    /// <summary>Verifica que, al filtrar por símbolo, el filtro se reenvíe correctamente al repositorio.</summary>
    [Fact]
    public async Task GetTransactionHistoryAsync_WhenFilteringBySymbol_ShouldForwardFilterToRepository()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _transactionRepository.Setup(r => r.SearchAsync(1, It.IsAny<TransactionFilterDto>())).ReturnsAsync((new List<TransactionDto>(), 0));

        var result = await CreateService().GetTransactionHistoryAsync(1, 1, new TransactionFilterDto { Symbol = "AAPL" });

        Assert.True(result.Success);
        _transactionRepository.Verify(r => r.SearchAsync(1, It.Is<TransactionFilterDto>(f => f.Symbol == "AAPL")), Times.Once);
    }

    /// <summary>Verifica que, ante una excepción inesperada, se registre el error y se devuelva una respuesta de error genérica.</summary>
    [Fact]
    public async Task GetTransactionHistoryAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _transactionRepository.Setup(r => r.SearchAsync(It.IsAny<int>(), It.IsAny<TransactionFilterDto>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().GetTransactionHistoryAsync(1, 1, new TransactionFilterDto());

        Assert.False(result.Success);
        Assert.Equal("Error al obtener las transacciones", result.Message);
    }
}
