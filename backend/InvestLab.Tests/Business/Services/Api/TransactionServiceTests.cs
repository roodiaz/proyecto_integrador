using InvestLab.Business.Services.Api;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Transaction;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static InvestLab.Models.Enums;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="TransactionService"/>, cubriendo el método invocado desde <c>PortfolioController</c>
/// para consultar el historial de transacciones del usuario.
/// </summary>
public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<TransactionService>> _logger = new();

    private TransactionService CreateService() => new(_transactionRepository.Object, _unitOfWork.Object, _logger.Object);

    private static TransactionDto TransactionEntity(string symbol = "AAPL", TransactionType type = TransactionType.Buy) =>
        new() { OperationDate = DateTime.UtcNow, Symbol = symbol, Type = type, Quantity = 5, BuyPrice = 150, Total = 750 };

    // ---------- GetTransactionHistoryAsync ----------

    /// <summary>Verifica que, cuando el usuario tiene transacciones, se devuelva una respuesta exitosa con los datos y el total de registros encontrados.</summary>
    [Fact]
    public async Task GetTransactionHistoryAsync_WhenUserHasTransactions_ShouldReturnSuccessResponseWithDataAndTotal()
    {
        var transactions = new List<TransactionDto> { TransactionEntity("AAPL"), TransactionEntity("MSFT", TransactionType.Sell) };
        _transactionRepository.Setup(r => r.SearchAsync(1, It.IsAny<TransactionFilterDto>())).ReturnsAsync((transactions, 2));

        var result = await CreateService().GetTransactionHistoryAsync(1, new TransactionFilterDto());

        Assert.True(result.Success);
        var data = Assert.IsType<TransactionSearchResponseDto>(result.Data);
        Assert.Equal(2, data.Total);
        Assert.Equal(transactions, data.Data);
    }

    /// <summary>Verifica que, cuando el usuario no tiene transacciones, se devuelva una respuesta exitosa con una colección vacía y total cero.</summary>
    [Fact]
    public async Task GetTransactionHistoryAsync_WhenUserHasNoTransactions_ShouldReturnSuccessResponseWithEmptyData()
    {
        _transactionRepository.Setup(r => r.SearchAsync(1, It.IsAny<TransactionFilterDto>())).ReturnsAsync((new List<TransactionDto>(), 0));

        var result = await CreateService().GetTransactionHistoryAsync(1, new TransactionFilterDto());

        Assert.True(result.Success);
        var data = Assert.IsType<TransactionSearchResponseDto>(result.Data);
        Assert.Empty(data.Data);
        Assert.Equal(0, data.Total);
    }

    /// <summary>Verifica que, al filtrar por símbolo, el filtro se reenvíe correctamente al repositorio.</summary>
    [Fact]
    public async Task GetTransactionHistoryAsync_WhenFilteringBySymbol_ShouldForwardFilterToRepository()
    {
        _transactionRepository.Setup(r => r.SearchAsync(1, It.IsAny<TransactionFilterDto>())).ReturnsAsync((new List<TransactionDto>(), 0));

        var result = await CreateService().GetTransactionHistoryAsync(1, new TransactionFilterDto { Symbol = "AAPL" });

        Assert.True(result.Success);
        _transactionRepository.Verify(r => r.SearchAsync(1, It.Is<TransactionFilterDto>(f => f.Symbol == "AAPL")), Times.Once);
    }

    /// <summary>Verifica que, ante una excepción inesperada, se registre el error y se devuelva una respuesta de error genérica.</summary>
    [Fact]
    public async Task GetTransactionHistoryAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _transactionRepository.Setup(r => r.SearchAsync(It.IsAny<int>(), It.IsAny<TransactionFilterDto>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().GetTransactionHistoryAsync(1, new TransactionFilterDto());

        Assert.False(result.Success);
        Assert.Equal("Error al obtener las transacciones", result.Message);
    }
}
