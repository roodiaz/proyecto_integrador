import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';

import { Alerts } from './alerts';
import { AlertService } from '../../services/alert.service';
import { NotificationService } from '../../services/notification.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { ApiResponse } from '../../../../core/models/api-response.model';
import { Alert, AlertSearchResponseDto, AlertStatsDto } from '../../models/alert.model';

describe('Alerts', () => {
  let component: Alerts;
  let fixture: ComponentFixture<Alerts>;
  let alertServiceSpy: jasmine.SpyObj<AlertService>;
  let notificationServiceSpy: jasmine.SpyObj<NotificationService>;
  let snackBarSpy: jasmine.SpyObj<SnackBarService>;
  let dialogOpenSpy: jasmine.Spy;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockSearchResponse: ApiResponse<AlertSearchResponseDto> = {
    success: true,
    message: 'ok',
    data: {
      data: [
        { id: 1, symbol: 'AAPL', conditionType: 1, operator: 1, value: 150, isActive: true, createdAt: new Date().toISOString() }
      ],
      total: 1
    }
  };

  const mockStatsResponse: ApiResponse<AlertStatsDto> = {
    success: true,
    message: 'ok',
    data: { active: 1, paused: 0, triggeredToday: 0, totalUsed: 1, limitAlerts: 10 }
  };

  function configure(queryParams: Record<string, string> = {}) {
    alertServiceSpy = jasmine.createSpyObj('AlertService', ['search', 'getStats', 'delete', 'toggle']);
    notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['getUnreadCount']);
    snackBarSpy = jasmine.createSpyObj('SnackBarService', ['success', 'error']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    alertServiceSpy.search.and.returnValue(of(mockSearchResponse));
    alertServiceSpy.getStats.and.returnValue(of(mockStatsResponse));
    notificationServiceSpy.getUnreadCount.and.returnValue(of({ success: true, message: 'ok', data: { count: 3 } }));

    return TestBed.configureTestingModule({
      imports: [Alerts],
      providers: [
        { provide: AlertService, useValue: alertServiceSpy },
        { provide: NotificationService, useValue: notificationServiceSpy },
        { provide: SnackBarService, useValue: snackBarSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: { queryParams: of(queryParams) } }
      ]
    }).compileComponents();
  }

  beforeEach(async () => {
    // Arrange
    await configure();

    fixture = TestBed.createComponent(Alerts);
    component = fixture.componentInstance;
    dialogOpenSpy = spyOn(fixture.debugElement.injector.get(MatDialog), 'open');
    fixture.detectChanges();
  });

  it('should create', () => {
    // Assert
    expect(component).toBeTruthy();
  });

  it('debe cargar alertas, estadísticas y notificaciones no leídas al inicializar', () => {
    // Assert
    expect(alertServiceSpy.search).toHaveBeenCalled();
    expect(alertServiceSpy.getStats).toHaveBeenCalled();
    expect(notificationServiceSpy.getUnreadCount).toHaveBeenCalled();

    expect(component.alerts.length).toBe(1);
    expect(component.alerts[0].symbol).toBe('AAPL');
    expect(component.totalAlerts).toBe(1);
    expect(component.activeAlerts).toBe(1);
    expect(component.unreadNotificationsCount).toBe(3);
  });

  it('loadAlerts() debe vaciar la lista si la respuesta no contiene datos', () => {
    // Arrange
    alertServiceSpy.search.and.returnValue(of({ success: false, message: 'error', data: null } as any));

    // Act
    component.loadAlerts();

    // Assert
    expect(component.alerts).toEqual([]);
    expect(component.totalAlerts).toBe(0);
  });

  it('loadAlerts() debe vaciar la lista si la petición falla', () => {
    // Arrange
    alertServiceSpy.search.and.returnValue(throwError(() => new Error('network error')));

    // Act
    component.loadAlerts();

    // Assert
    expect(component.alerts).toEqual([]);
  });

  it('applyFilter() debe reiniciar la paginación a la página 1 y recargar las alertas', () => {
    // Arrange
    component.currentPage = 3;
    alertServiceSpy.search.calls.reset();

    // Act
    component.applyFilter();

    // Assert
    expect(component.currentPage).toBe(1);
    expect(alertServiceSpy.search).toHaveBeenCalled();
  });

  it('clearSearch() debe limpiar todos los filtros y recargar', () => {
    // Arrange
    component.searchTerm = 'AAPL';
    component.statusFilter = 'active';
    component.createdFrom = '2026-01-01';
    component.createdTo = '2026-01-31';

    // Act
    component.clearSearch();

    // Assert
    expect(component.searchTerm).toBe('');
    expect(component.statusFilter).toBe('');
    expect(component.createdFrom).toBe('');
    expect(component.createdTo).toBe('');
  });

  it('setActiveView() debe cambiar la vista activa y recargar notificaciones no leídas al ir a historial', () => {
    // Arrange
    notificationServiceSpy.getUnreadCount.calls.reset();

    // Act
    component.setActiveView('history');

    // Assert
    expect(component.activeView).toBe('history');
    expect(notificationServiceSpy.getUnreadCount).toHaveBeenCalled();
  });

  it('toggleAlert() debe recargar alertas y estadísticas, y notificar el éxito al activar correctamente', () => {
    // Arrange
    alertServiceSpy.toggle.and.returnValue(of({ success: true, message: 'ok', data: null } as ApiResponse));
    const alert = { ...component.alerts[0] };

    // Act
    component.toggleAlert(alert, true);

    // Assert
    expect(alertServiceSpy.toggle).toHaveBeenCalledWith(alert.id);
    expect(snackBarSpy.success).toHaveBeenCalledWith('Alerta activada');
  });

  it('toggleAlert() debe revertir el estado local y notificar el error si la operación falla', () => {
    // Arrange
    alertServiceSpy.toggle.and.returnValue(throwError(() => ({ error: { message: 'No se pudo actualizar' } })));
    const alert: Alert = { ...component.alerts[0], isActive: true };

    // Act
    component.toggleAlert(alert, true);

    // Assert
    expect(alert.isActive).toBeFalse();
    expect(snackBarSpy.error).toHaveBeenCalledWith('No se pudo actualizar');
  });

  it('deleteAlert() debe eliminar la alerta y recargar listas cuando el usuario confirma', async () => {
    // Arrange
    dialogOpenSpy.and.returnValue({ afterClosed: () => of(true) } as any);
    alertServiceSpy.delete.and.returnValue(of({ success: true, message: 'ok', data: null } as ApiResponse));
    alertServiceSpy.search.calls.reset();

    // Act
    await component.deleteAlert(component.alerts[0]);

    // Assert
    expect(alertServiceSpy.delete).toHaveBeenCalledWith(component.alerts[0].id);
    expect(snackBarSpy.success).toHaveBeenCalledWith('Alerta eliminada correctamente');
    expect(alertServiceSpy.search).toHaveBeenCalled();
  }, 15000);

  it('deleteAlert() no debe eliminar nada si el usuario cancela la confirmación', async () => {
    // Arrange
    dialogOpenSpy.and.returnValue({ afterClosed: () => of(false) } as any);

    // Act
    await component.deleteAlert(component.alerts[0]);

    // Assert
    expect(alertServiceSpy.delete).not.toHaveBeenCalled();
  }, 15000);

  it('getConditionDisplay() debe devolver la etiqueta legible de una condición conocida', () => {
    // Assert
    expect(component.getConditionDisplay('>')).toBe('Precio mayor que');
  });

  it('getTargetDisplay() debe formatear el valor según el tipo de condición', () => {
    // Assert
    expect(component.getTargetDisplay({ condition: '>', price: 150 } as Alert)).toBe('$150.00');
    expect(component.getTargetDisplay({ condition: '%>', percentChange: 5 } as Alert)).toBe('5%');
  });
});
