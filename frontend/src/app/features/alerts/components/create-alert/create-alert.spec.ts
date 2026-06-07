import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';

import { CreateAlertComponent } from './create-alert';
import { AlertService } from '../../services/alert.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { Alert } from '../../models/alert.model';
import { ApiResponse } from '../../../../core/models/api-response.model';

describe('CreateAlertComponent', () => {
  let component: CreateAlertComponent;
  let fixture: ComponentFixture<CreateAlertComponent>;
  let alertServiceSpy: jasmine.SpyObj<AlertService>;
  let snackBarSpy: jasmine.SpyObj<SnackBarService>;
  let dialogRefSpy: jasmine.SpyObj<MatDialogRef<CreateAlertComponent>>;

  const validValue: { symbol: string; condition: string; price: number | null; percentChange: number | null; isActive: boolean } =
    { symbol: 'AAPL', condition: '>', price: 100, percentChange: null, isActive: true };

  function configure(dialogData: { alert?: Alert } = {}) {
    alertServiceSpy = jasmine.createSpyObj('AlertService', ['create', 'update']);
    snackBarSpy = jasmine.createSpyObj('SnackBarService', ['success', 'error']);
    dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

    return TestBed.configureTestingModule({
      imports: [CreateAlertComponent, ReactiveFormsModule],
      providers: [
        { provide: AlertService, useValue: alertServiceSpy },
        { provide: SnackBarService, useValue: snackBarSpy },
        { provide: MatDialogRef, useValue: dialogRefSpy },
        { provide: MAT_DIALOG_DATA, useValue: dialogData }
      ]
    }).compileComponents();
  }

  describe('modo creación', () => {
    beforeEach(async () => {
      // Arrange
      await configure({});

      fixture = TestBed.createComponent(CreateAlertComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should create', () => {
      // Assert
      expect(component).toBeTruthy();
    });

    it('debe inicializar el formulario en modo creación con valores por defecto', () => {
      // Assert
      expect(component.isEditMode).toBeFalse();
      expect(component.alertForm.value.condition).toBe('<');
      expect(component.alertForm.value.isActive).toBeTrue();
    });

    it('isPercentageCondition() debe identificar condiciones porcentuales', () => {
      // Act
      component.alertForm.get('condition')?.setValue('%>');
      // Assert
      expect(component.isPercentageCondition()).toBeTrue();

      // Act
      component.alertForm.get('condition')?.setValue('>');
      // Assert
      expect(component.isPercentageCondition()).toBeFalse();
    });

    it('no debe llamar al AlertService si el formulario es inválido', () => {
      // Arrange
      component.alertForm.setValue({ symbol: '', condition: '>', price: null, percentChange: null, isActive: true });

      // Act
      component.onSubmit();

      // Assert
      expect(alertServiceSpy.create).not.toHaveBeenCalled();
    });

    it('debe llamar a AlertService.create() con el dto correcto y cerrar el diálogo cuando es exitoso', () => {
      // Arrange
      const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };
      alertServiceSpy.create.and.returnValue(of(mockResponse));
      component.alertForm.setValue(validValue);

      // Act
      component.onSubmit();

      // Assert
      const expectedDto = { symbol: 'AAPL', condition: '>', price: 100, percentChange: null, isActive: true };
      expect(alertServiceSpy.create).toHaveBeenCalledWith(expectedDto as any);
      expect(snackBarSpy.success).toHaveBeenCalled();
      expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });

    it('debe notificar el error y no cerrar el diálogo si AlertService.create() falla', () => {
      // Arrange
      alertServiceSpy.create.and.returnValue(throwError(() => ({ error: { message: 'No se pudo crear' } })));
      component.alertForm.setValue(validValue);

      // Act
      component.onSubmit();

      // Assert
      expect(snackBarSpy.error).toHaveBeenCalledWith('No se pudo crear');
      expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });

    it('onCancel() debe cerrar el diálogo sin guardar', () => {
      // Act
      component.onCancel();

      // Assert
      expect(dialogRefSpy.close).toHaveBeenCalledWith();
    });

    it('debe ajustar los validadores al cambiar a una condición porcentual', () => {
      // Act
      component.alertForm.get('condition')?.setValue('%>');

      // Assert
      expect(component.alertForm.get('price')?.value).toBeNull();
      expect(component.alertForm.get('percentChange')?.validator).not.toBeNull();

      // Act
      component.alertForm.get('percentChange')?.setValue(150);
      // Assert
      expect(component.alertForm.get('percentChange')?.hasError('max')).toBeTrue();
    });
  });

  describe('modo edición', () => {
    const existingAlert: Alert = {
      id: 5,
      symbol: 'TSLA',
      condition: '<',
      price: 200,
      isActive: false,
      createdAt: new Date(),
      updatedAt: new Date(),
      userId: 'u1'
    };

    beforeEach(async () => {
      // Arrange
      await configure({ alert: existingAlert });

      fixture = TestBed.createComponent(CreateAlertComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('debe entrar en modo edición y precargar el formulario con los datos de la alerta', () => {
      // Assert
      expect(component.isEditMode).toBeTrue();
      expect(component.alertForm.value.symbol).toBe('TSLA');
      expect(component.alertForm.value.price).toBe(200);
      expect(component.alertForm.value.isActive).toBeFalse();
    });

    it('debe llamar a AlertService.update() con el id existente y cerrar el diálogo cuando es exitoso', () => {
      // Arrange
      const mockResponse: ApiResponse<any> = { success: true, message: 'ok', data: {} };
      alertServiceSpy.update.and.returnValue(of(mockResponse));

      // Act
      component.onSubmit();

      // Assert
      const expectedDto = { id: 5, symbol: 'TSLA', condition: '<', price: 200, percentChange: null, isActive: false };
      expect(alertServiceSpy.update).toHaveBeenCalledWith(expectedDto as any);
      expect(snackBarSpy.success).toHaveBeenCalled();
      expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });
  });
});
