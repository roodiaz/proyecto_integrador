using InvestLab.Business.Interfaces;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Alerts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using static InvestLab.Models.Enums;

namespace InvestLab.Business.Services
{
    public class AlertService : IAlertService
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IUserSettingRepository _userSettingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AlertService> _logger;

        public AlertService(IAlertRepository alertRepository, IAssetRepository assetRepository, IUserSettingRepository userSettingRepository, IUnitOfWork unitOfWork, ILogger<AlertService> logger)
        {
            _alertRepository = alertRepository;
            _assetRepository = assetRepository;
            _userSettingRepository = userSettingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Response> CreateAlertAsync(int userId, CreateAlertDto dto)
        {
            try
            {
                if (!IsValidCondition(dto))
                    return Response.Fail("Datos inválidos");

                var asset = await _assetRepository.GetBySymbolAsync(dto.Symbol);

                if (asset == null)
                    return Response.Fail("Activo no encontrado");

                var settings = await _userSettingRepository.GetByUserIdAsync(userId);

                var count = await _alertRepository.CountByUserAsync(userId);

                if (count >= settings.MaxAlerts)
                    return Response.Fail("Límite de alertas alcanzado");

                var (type, op, value) = MapCondition(dto);

                var exists = await _alertRepository.ExistsAsync(userId, asset.Id, type, op, value);

                if (exists)
                    return Response.Fail("Ya existe una alerta igual");

                var alert = new Alert
                {
                    UserId = userId,
                    AssetId = asset.Id,
                    ConditionType = (short)type,
                    Operator = (short)op,
                    Value = value,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                await _alertRepository.AddAsync(alert);
                await _unitOfWork.SaveChangesAsync();

                return Response.Ok(null, "Alerta creada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CreateAlert");
                return Response.Fail("Error interno");
            }
        }

        public async Task<Response> DeleteAlertAsync(int userId, int id)
        {
            var alert = await _alertRepository.GetByIdAsync(id);

            if (alert == null || alert.UserId != userId)
                return Response.Fail("No encontrada");

            await _alertRepository.DeleteAsync(alert);
            await _unitOfWork.SaveChangesAsync();

            return Response.Ok(null, "Eliminada");
        }

        public async Task<Response> ToggleAlertAsync(int userId, int id)
        {
            var alert = await _alertRepository.GetByIdAsync(id);

            if (alert == null || alert.UserId != userId)
                return Response.Fail("No encontrada");

            alert.IsActive = !alert.IsActive;

            await _unitOfWork.SaveChangesAsync();

            return Response.Ok(null, "Estado actualizado");
        }

        public async Task<Response> GetAlertsAsync(int userId, AlertFilterDto filter)
        {
            var (alerts, total) = await _alertRepository.GetPagedAsync(userId, filter);

            return Response.Ok(new
            {
                data = alerts.Select(a => new
                {
                    a.Id,
                    symbol = a.Asset.Symbol,
                    value = a.Value,
                    isActive = a.IsActive,
                    createdAt = a.CreatedAt
                }),
                total
            });
        }

        public async Task<Response> UpdateAlertAsync(int userId, UpdateAlertDto dto)
        {
            try
            {
                var alert = await _alertRepository.GetByIdAsync(dto.Id);

                if (alert == null || alert.UserId != userId)
                    return Response.Fail("Alerta no encontrada");

                if (!IsValidCondition(dto))
                    return Response.Fail("Datos inválidos");

                var asset = await _assetRepository.GetBySymbolAsync(dto.Symbol);

                if (asset == null)
                    return Response.Fail("Activo no encontrado");

                var (type, op, value) = MapCondition(dto);

                var exists = await _alertRepository.ExistsAsync(userId, asset.Id, type, op, value);

                if (exists && (
                    alert.AssetId != asset.Id ||
                    alert.ConditionType != (short)type ||
                    alert.Operator != (short)op ||
                    alert.Value != value))
                {
                    return Response.Fail("Ya existe una alerta igual");
                }

                alert.AssetId = asset.Id;
                alert.ConditionType = (short)type;
                alert.Operator = (short)op;
                alert.Value = value;
                alert.IsActive = dto.IsActive;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Alerta actualizada {AlertId}", alert.Id);

                return Response.Ok(null, "Alerta actualizada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UpdateAlert");
                return Response.Fail("Error interno");
            }
        }

        public async Task<Response> GetStatsAsync(int userId)
        {
            try
            {
                var (active, paused, triggeredToday, total) =
                    await _alertRepository.GetStatsAsync(userId);

                return Response.Ok(new
                {
                    active,
                    paused,
                    triggeredToday,
                    total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetStats");
                return Response.Fail("Error interno");
            }
        }

        // Helpers
        private bool IsValidCondition(CreateAlertDto dto)
        {
            return dto.Condition switch
            {
                ">" or "<" => dto.Price.HasValue,
                "%>" or "%<" => dto.PercentChange.HasValue,
                _ => false
            };
        }

        private (ConditionType, OperatorType, decimal) MapCondition(CreateAlertDto dto)
        {
            return dto.Condition switch
            {
                ">" => (ConditionType.Price, OperatorType.GreaterThan, dto.Price!.Value),
                "<" => (ConditionType.Price, OperatorType.LessThan, dto.Price!.Value),
                "%>" => (ConditionType.Percentage, OperatorType.GreaterThan, dto.PercentChange!.Value),
                "%<" => (ConditionType.Percentage, OperatorType.LessThan, dto.PercentChange!.Value),
                _ => throw new ArgumentException()
            };
        }
    }
}
