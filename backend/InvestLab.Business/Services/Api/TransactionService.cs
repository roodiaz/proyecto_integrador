using ClosedXML.Excel;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Transaction;
using Microsoft.Extensions.Logging;
using static InvestLab.Models.Enums;
using static InvestLab.Models.MessageCodes;

namespace InvestLab.Business.Services.Api
{
    public class TransactionService : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUserPortfolioRepository _userPortfolioRepository;
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de transacciones con sus dependencias.
        /// </summary>
        /// <param name="transactionRepository">Repositorio utilizado para acceder y consultar las transacciones.</param>
        /// <param name="userPortfolioRepository">Repositorio utilizado para validar la pertenencia del portfolio al usuario.</param>
        /// <param name="unitOfWork">Unidad de trabajo para coordinar operaciones de persistencia.</param>
        /// <param name="logger">Registrador de eventos para el servicio de transacciones.</param>
        public TransactionService(ITransactionRepository transactionRepository, IUserPortfolioRepository userPortfolioRepository, IUnitOfWork unitOfWork, ILogger<TransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _userPortfolioRepository = userPortfolioRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el historial de transacciones de un portfolio aplicando los filtros indicados.
        /// </summary>
        /// <param name="userId">Identificador del usuario dueño del portfolio.</param>
        /// <param name="portfolioId">Identificador del portfolio cuyas transacciones se desean consultar.</param>
        /// <param name="filter">Criterios de filtrado y paginación a aplicar sobre la búsqueda de transacciones.</param>
        /// <returns>Una respuesta con los datos y el total de transacciones encontradas, o un mensaje de error si la operación falla.</returns>
        public async Task<Response> GetTransactionHistoryAsync(int userId, int portfolioId, TransactionFilterDto filter)
        {
            try
            {
                var portfolio = await _userPortfolioRepository.GetByIdAndUserAsync(portfolioId, userId);
                if (portfolio == null)
                {
                    _logger.LogWarning("Portfolio no encontrado: UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);
                    return Response.Fail("Portfolio no encontrado", PORTFOLIO_NOT_FOUND);
                }

                var result = await _transactionRepository.SearchAsync(portfolioId, filter);

                return Response.Ok(
                    new TransactionSearchResponseDto
                    {
                        Data = result.Data,
                        Total = result.Total
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo transacciones para portfolio {PortfolioId}", portfolioId);
                return Response.Fail("Error al obtener las transacciones");
            }
        }

        public async Task<byte[]> ExportTransactionsToExcelAsync(int userId, int portfolioId, TransactionFilterDto filter)
        {
            var portfolio = await _userPortfolioRepository.GetByIdAndUserAsync(portfolioId, userId);
            if (portfolio == null)
                return [];

            var transactions = await _transactionRepository.GetAllForExportAsync(portfolioId, filter);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Operaciones");

            string[] headers = ["Fecha", "Ticker", "Sector", "Tipo", "Cantidad", "Precio (USD)", "Total (USD)"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int r = 0; r < transactions.Count; r++)
            {
                var t = transactions[r];
                int row = r + 2;
                sheet.Cell(row, 1).Value = t.OperationDate.ToString("dd/MM/yyyy HH:mm");
                sheet.Cell(row, 2).Value = t.Symbol;
                sheet.Cell(row, 3).Value = t.Sector ?? "-";
                sheet.Cell(row, 4).Value = t.Type == TransactionType.Buy ? "Compra" : "Venta";
                sheet.Cell(row, 5).Value = t.Quantity;
                sheet.Cell(row, 6).Value = t.BuyPrice;
                sheet.Cell(row, 7).Value = t.Total;

                for (int c = 1; c <= 7; c++)
                    sheet.Cell(row, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var typeCell = sheet.Cell(row, 4);
                typeCell.Style.Font.FontColor = t.Type == TransactionType.Buy
                    ? XLColor.FromHtml("#1B8436")
                    : XLColor.FromHtml("#C0392B");
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
