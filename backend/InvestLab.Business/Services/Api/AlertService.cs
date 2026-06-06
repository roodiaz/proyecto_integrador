using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Alerts;
using InvestLab.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using static InvestLab.Models.Enums;

namespace InvestLab.Business.Services.Api
{
    public class AlertService : IAlertService
    {
        private readonly LimitsOptions _limits;
        private readonly IAlertRepository _alertRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IUserSettingRepository _userSettingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAssetService _assetService;
        private readonly ILogger<AlertService> _logger;

        public AlertService(IAlertRepository alertRepository, IAssetRepository assetRepository, IUserSettingRepository userSettingRepository, IUnitOfWork unitOfWork, ILogger<AlertService> logger, IOptions<LimitsOptions> options, IAssetService assetService)
        {
            _alertRepository = alertRepository;
            _assetRepository = assetRepository;
            _userSettingRepository = userSettingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _limits = options.Value;
            _assetService = assetService;
        }

        public async Task<Response> CreateAlertAsync(int userId, CreateAlertDto dto)
        {
            try
            {
                var settings = await _userSettingRepository.GetByUserIdAsync(userId);
                if (settings == null)
                    return Response.Fail("Configuración de usuario no encontrada");

                if (settings.AlertsUsed >= _limits.MaxAlerts)
                    return Response.Fail("Límite de alertas alcanzado");

                var asset = await _assetService.GetOrCreateAsync(dto.Symbol);
                if (asset == null)
                    return Response.Fail("Activo no encontrado");

                var condition = MapCondition(dto);

                var alert = new Alert
                {
                    UserId = userId,
                    AssetId = asset.Id,
                    ConditionType = condition.Item1,
                    Operator = condition.Item2,
                    Value = condition.Item3,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                await _alertRepository.AddAsync(alert);

                settings.AlertsUsed++;

                await _unitOfWork.SaveChangesAsync();

                return Response.Ok("Alerta creada correctamente");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CreateAlertAsync");
                return Response.Fail("Error interno");
            }
        }

        public async Task<Response> DeleteAlertAsync(int userId, int id)
        {
            try
            {
                var alert = await _alertRepository.GetByIdAsync(id);
                if (alert == null || alert.UserId != userId)
                    return Response.Fail("No encontrada");

                var settings = await _userSettingRepository.GetByUserIdAsync(userId);

                await _alertRepository.DeleteAsync(alert);

                if (settings != null && settings.AlertsUsed > 0)
                    settings.AlertsUsed--;

                await _unitOfWork.SaveChangesAsync();

                return Response.Ok(null, "Eliminada");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DeleteAlertAsync");
                return Response.Fail("Error interno");
            }
        }

        public async Task<Response> ToggleAlertAsync(int userId, int id)
        {
            try
            {
                var alert = await _alertRepository.GetByIdAsync(id);

                if (alert == null || alert.UserId != userId)
                    return Response.Fail("No encontrada");

                alert.IsActive = !alert.IsActive;

                await _unitOfWork.SaveChangesAsync();

                return Response.Ok(null, "Estado actualizado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ToggleAlertAsync");
                return Response.Fail("Error interno");
            }
        }

        public async Task<Response> GetAlertsAsync(int userId, AlertFilterDto filter)
        {
            try
            {
                var (alerts, total) = await _alertRepository.GetPagedAsync(userId, filter);

                return Response.Ok(new
                {
                    data = alerts.Select(a => new
                    {
                        a.Id,
                        symbol = a.Asset.Symbol,
                        conditionType = a.ConditionType,
                        @operator = a.Operator,
                        value = a.Value,
                        isActive = a.IsActive,
                        createdAt = a.CreatedAt,
                        lastTriggered = a.LastTriggered
                    }),
                    total
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAlertsAsync");
                return Response.Fail("Error interno");
            }
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

                var asset = await _assetRepository.GetAsync(dto.Symbol);

                if (asset == null)
                    return Response.Fail("Activo no encontrado");

                var (type, op, value) = MapCondition(dto);

                var exists = await _alertRepository.ExistsAsync(userId, asset.Id, type, op, value);

                if (exists && (
                    alert.AssetId != asset.Id ||
                    alert.ConditionType != type ||
                    alert.Operator != op ||
                    alert.Value != value))
                {
                    return Response.Fail("Ya existe una alerta igual");
                }

                alert.AssetId = asset.Id;
                alert.ConditionType = type;
                alert.Operator = op;
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
                var (active, paused, triggeredToday, totalUsed) =
                    await _alertRepository.GetStatsAsync(userId);

                return Response.Ok(new
                {
                    active,
                    paused,
                    triggeredToday,
                    totalUsed,
                    limitAlerts = _limits.MaxAlerts
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

        private (ConditionType, AlertOperator, decimal) MapCondition(CreateAlertDto dto)
        {
            return dto.Condition switch
            {
                ">" => (ConditionType.Price, AlertOperator.GreaterThan, dto.Price!.Value),
                "<" => (ConditionType.Price, AlertOperator.LessThan, dto.Price!.Value),
                ">=" => (ConditionType.Price, AlertOperator.GreaterThanOrEqual, dto.Price!.Value),
                "<=" => (ConditionType.Price, AlertOperator.LessThanOrEqual, dto.Price!.Value),
                "=" => (ConditionType.Price, AlertOperator.Equal, dto.Price!.Value),
                "%>" => (ConditionType.Percentage, AlertOperator.GreaterThan, dto.PercentChange!.Value),
                "%<" => (ConditionType.Percentage, AlertOperator.LessThan, dto.PercentChange!.Value),
                _ => throw new ArgumentException("Condición inválida")
            };
        }
    }
}
