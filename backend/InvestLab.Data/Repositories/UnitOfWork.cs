using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace InvestLab.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly InvestLabDbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="UnitOfWork"/> con el contexto de base de datos especificado.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab utilizado para realizar las operaciones.</param>
        public UnitOfWork(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Guarda de forma asincrónica todos los cambios pendientes realizados en el contexto de base de datos.
        /// </summary>
        /// <returns>Una tarea que representa la operación asincrónica de guardado.</returns>
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
