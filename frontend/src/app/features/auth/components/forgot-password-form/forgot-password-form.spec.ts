import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { ForgotPasswordForm } from './forgot-password-form';
import { AuthService } from '../../services/auth.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { ApiResponse } from '../../../../core/models/api-response.model';
import { ForgotPasswordResponse } from '../../models/forgot-password.model';

describe('ForgotPasswordForm', () => {
  let component: ForgotPasswordForm;
  let fixture: ComponentFixture<ForgotPasswordForm>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let snackBarSpy: jasmine.SpyObj<SnackBarService>;
  let router: Router;

  const validResetValue = {
    code: '123456',
    newPassword: 'newSecret1',
    confirmPassword: 'newSecret1'
  };

  beforeEach(async () => {
    // Arrange
    authServiceSpy = jasmine.createSpyObj('AuthService', ['forgotPassword', 'resetPassword']);
    snackBarSpy = jasmine.createSpyObj('SnackBarService', ['success', 'error']);

    await TestBed.configureTestingModule({
      imports: [ForgotPasswordForm, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: SnackBarService, useValue: snackBarSpy },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ForgotPasswordForm);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  it('should create', () => {
    // Assert
    expect(component).toBeTruthy();
  });

  it('debe inicializar el formulario de email vacío e inválido', () => {
    // Assert
    expect(component.emailForm.invalid).toBeTrue();
    expect(component.codeSent).toBeFalse();
  });

  it('debe marcar el formulario de reseteo como inválido si las contraseñas no coinciden', () => {
    // Act
    component.resetForm.setValue({ ...validResetValue, confirmPassword: 'distinta' });

    // Assert
    expect(component.resetForm.hasError('mismatch')).toBeTrue();
  });

  it('no debe llamar a AuthService.forgotPassword y debe marcar los campos como tocados si el email es inválido', () => {
    // Act
    component.onSendCode();

    // Assert
    expect(authServiceSpy.forgotPassword).not.toHaveBeenCalled();
    expect(component.email?.touched).toBeTrue();
  });

  it('debe llamar a AuthService.forgotPassword() y avanzar al paso de reseteo cuando el envío es exitoso', () => {
    // Arrange
    const mockResponse: ApiResponse<ForgotPasswordResponse> = {
      success: true,
      message: 'Se envió un código de recuperación a tu correo electrónico',
      data: { email: 'user@test.com', emailSent: true }
    };
    authServiceSpy.forgotPassword.and.returnValue(of(mockResponse));
    component.emailForm.setValue({ email: 'user@test.com' });

    // Act
    component.onSendCode();

    // Assert
    expect(authServiceSpy.forgotPassword).toHaveBeenCalledWith({ email: 'user@test.com' });
    expect(component.codeSent).toBeTrue();
    expect(component.sentToEmail).toBe('user@test.com');
    expect(snackBarSpy.success).toHaveBeenCalled();
    expect(component.isLoading).toBeFalse();
  });

  it('debe mostrar un error y no avanzar de paso cuando el backend responde sin éxito', () => {
    // Arrange
    const mockResponse: ApiResponse<ForgotPasswordResponse> = {
      success: false,
      message: 'No existe una cuenta asociada a ese email',
      data: null
    };
    authServiceSpy.forgotPassword.and.returnValue(of(mockResponse));
    component.emailForm.setValue({ email: 'user@test.com' });

    // Act
    component.onSendCode();

    // Assert
    expect(component.codeSent).toBeFalse();
    expect(snackBarSpy.error).toHaveBeenCalledWith('No existe una cuenta asociada a ese email');
  });

  it('debe mostrar el mensaje de error del backend cuando AuthService.forgotPassword falla', () => {
    // Arrange
    authServiceSpy.forgotPassword.and.returnValue(throwError(() => ({ error: { message: 'No existe una cuenta asociada a ese email' } })));
    component.emailForm.setValue({ email: 'user@test.com' });

    // Act
    component.onSendCode();

    // Assert
    expect(snackBarSpy.error).toHaveBeenCalledWith('No existe una cuenta asociada a ese email');
    expect(component.isLoading).toBeFalse();
  });

  it('no debe llamar a AuthService.resetPassword y debe marcar los campos como tocados si el formulario de reseteo es inválido', () => {
    // Act
    component.onResetPassword();

    // Assert
    expect(authServiceSpy.resetPassword).not.toHaveBeenCalled();
    expect(component.code?.touched).toBeTrue();
  });

  it('debe llamar a AuthService.resetPassword() y navegar a /login cuando el reseteo es exitoso', () => {
    // Arrange
    const mockResponse: ApiResponse<null> = { success: true, message: 'Contraseña actualizada correctamente', data: null };
    authServiceSpy.resetPassword.and.returnValue(of(mockResponse));
    component.sentToEmail = 'user@test.com';
    component.resetForm.setValue(validResetValue);

    // Act
    component.onResetPassword();

    // Assert
    expect(authServiceSpy.resetPassword).toHaveBeenCalledWith({
      email: 'user@test.com',
      code: validResetValue.code,
      newPassword: validResetValue.newPassword,
      confirmPassword: validResetValue.confirmPassword
    });
    expect(snackBarSpy.success).toHaveBeenCalledWith('Contraseña actualizada correctamente');
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
    expect(component.isLoading).toBeFalse();
  });

  it('debe mostrar un error y no navegar cuando el backend responde sin éxito al restablecer la contraseña', () => {
    // Arrange
    const mockResponse: ApiResponse<null> = { success: false, message: 'Código inválido', data: null };
    authServiceSpy.resetPassword.and.returnValue(of(mockResponse));
    component.sentToEmail = 'user@test.com';
    component.resetForm.setValue(validResetValue);

    // Act
    component.onResetPassword();

    // Assert
    expect(snackBarSpy.error).toHaveBeenCalledWith('Código inválido');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('debe mostrar el mensaje de error del backend cuando AuthService.resetPassword falla', () => {
    // Arrange
    authServiceSpy.resetPassword.and.returnValue(throwError(() => ({ error: { message: 'Código inválido o expirado' } })));
    component.sentToEmail = 'user@test.com';
    component.resetForm.setValue(validResetValue);

    // Act
    component.onResetPassword();

    // Assert
    expect(snackBarSpy.error).toHaveBeenCalledWith('Código inválido o expirado');
    expect(component.isLoading).toBeFalse();
  });
});
