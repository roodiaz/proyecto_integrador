using static InvestLab.Models.Enums;

namespace InvestLab.Models.DTOs.Transaction
{
    /// <summary>
    /// Operación de compra o venta registrada en el historial de un portfolio.
    /// </summary>
    public class TransactionDto
    {
        /// <summary>Fecha y hora en que se realizó la operación.</summary>
        public DateTime OperationDate { get; set; }

        /// <summary>Símbolo (ticker) del activo operado.</summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>Sector al que pertenece el activo.</summary>
        public string? Sector { get; set; }

        /// <summary>Tipo de operación (compra o venta).</summary>
        public TransactionType Type { get; set; }

        /// <summary>Cantidad de unidades operadas.</summary>
        public decimal Quantity { get; set; }

        /// <summary>Precio unitario al momento de la operación.</summary>
        public decimal BuyPrice { get; set; }

        /// <summary>Monto total de la operación.</summary>
        public decimal Total { get; set; }
    }
}
