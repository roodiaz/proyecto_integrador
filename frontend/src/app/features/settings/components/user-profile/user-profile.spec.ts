import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';

import { UserProfile } from './user-profile';
import { UserService } from '../../services/user.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { ProfileResponse } from '../../models/user-profile.model';
import { ApiResponse } from '../../../../core/models/api-response.model';

describe('UserProfile', () => {
  let component: UserProfile;
  let fixture: ComponentFixture<UserProfile>;
  let userServiceSpy: jasmine.SpyObj<UserService>;
  let snackBarSpy: jasmine.SpyObj<SnackBarService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let dialogSpy: jasmine.SpyObj<MatDialog>;

  const mockProfile: ProfileResponse = {
    success: true,
    message: 'ok',
    data: {
      id: 1,
      username: 'tester',
      email: 'tester@test.com',
      phone: '123',
      birthDate: null,
      profileImageUrl: null,
      settings: { currency: 'USD', emailNotifications: true }
    }
  };

  beforeEach(async () => {
    // Arrange
    userServiceSpy = jasmine.createSpyObj('UserService', ['getProfile', 'updateProfile', 'changePassword', 'uploadProfileImage', 'deleteAccount']);
    snackBarSpy = jasmine.createSpyObj('SnackBarService', ['success', 'error']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);

    userServiceSpy.getProfile.and.returnValue(of(mockProfile));

    await TestBed.configureTestingModule({
      imports: [UserProfile, ReactiveFormsModule],
      providers: [
        { provide: UserService, useValue: userServiceSpy },
        { provide: SnackBarService, useValue: snackBarSpy },
        { provide: Router, useValue: routerSpy },
        { provide: MatDialog, useValue: dialogSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UserProfile);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    // Assert
    expect(component).toBeTruthy();
  });

  it('debe cargar los datos del usuario al inicializar y completar el formulario de perfil', () => {
    // Assert
    expect(userServiceSpy.getProfile).toHaveBeenCalled();
    expect(component.profileForm.value.userName).toBe('tester');
    expect(component.profileForm.value.email).toBe('tester@test.com');
    expect(component.profileForm.value.currency).toBe('USD');
  });

  it('no debe guardar el perfil ni llamar al service si el formulario es inválido', () => {
    // Arrange
    component.profileForm.get('userName')?.setValue('');

    // Act
    component.onSaveProfile();

    // Assert
    expect(userServiceSpy.updateProfile).not.toHaveBeenCalled();
    expect(snackBarSpy.error).toHaveBeenCalled();
  });

  it('debe llamar a UserService.updateProfile() y notificar el éxito cuando el formulario es válido', () => {
    // Arrange
    const mockResponse: ApiResponse = { success: true, message: 'Perfil actualizado', data: null };
    userServiceSpy.updateProfile.and.returnValue(of(mockResponse));

    // Act
    component.onSaveProfile();

    // Assert
    expect(userServiceSpy.updateProfile).toHaveBeenCalled();
    expect(snackBarSpy.success).toHaveBeenCalledWith('Perfil actualizado');
    expect(component.savingProfile).toBeFalse();
  });

  it('debe notificar el error si la actualización del perfil falla', () => {
    // Arrange
    userServiceSpy.updateProfile.and.returnValue(throwError(() => ({ error: { message: 'Error al guardar' } })));

    // Act
    component.onSaveProfile();

    // Assert
    expect(snackBarSpy.error).toHaveBeenCalledWith('Error al guardar');
  });

  it('passwordMatchValidator() debe marcar error de incompatibilidad si las contraseñas no coinciden', () => {
    // Arrange
    component.passwordForm.setValue({ currentPassword: 'old', newPassword: 'abc123', confirmPassword: 'xyz789' });

    // Act
    component.passwordMatchValidator(component.passwordForm);

    // Assert
    expect(component.passwordForm.get('confirmPassword')?.errors).toEqual({ passwordMismatch: true });
  });

  it('passwordMatchValidator() no debe marcar error si las contraseñas coinciden', () => {
    // Arrange
    component.passwordForm.setValue({ currentPassword: 'old', newPassword: 'abc123', confirmPassword: 'abc123' });

    // Act
    component.passwordMatchValidator(component.passwordForm);

    // Assert
    expect(component.passwordForm.get('confirmPassword')?.errors).toBeNull();
  });

  it('no debe llamar a UserService.changePassword() si el formulario es inválido', () => {
    // Arrange
    component.passwordForm.setValue({ currentPassword: 'old', newPassword: 'abc123', confirmPassword: 'xyz789' });
    component.passwordForm.get('confirmPassword')?.setErrors({ passwordMismatch: true });

    // Act
    component.onChangePassword();

    // Assert
    expect(userServiceSpy.changePassword).not.toHaveBeenCalled();
  });

  it('debe llamar a UserService.changePassword(), notificar el éxito y reiniciar el formulario', () => {
    // Arrange
    const mockResponse: ApiResponse = { success: true, message: 'Contraseña actualizada', data: null };
    userServiceSpy.changePassword.and.returnValue(of(mockResponse));
    component.passwordForm.setValue({ currentPassword: 'old', newPassword: 'abc123', confirmPassword: 'abc123' });

    // Act
    component.onChangePassword();

    // Assert
    expect(userServiceSpy.changePassword).toHaveBeenCalledWith(jasmine.objectContaining({ currentPassword: 'old', newPassword: 'abc123', confirmPassword: 'abc123' }) as any);
    expect(snackBarSpy.success).toHaveBeenCalledWith('Contraseña actualizada');
    expect(component.passwordForm.value.currentPassword).toBeNull();
  });

  it('deleteAccount() debe llamar al service, limpiar el storage y navegar al login cuando es exitoso', () => {
    // Arrange
    const mockResponse: ApiResponse = { success: true, message: 'Cuenta eliminada', data: null };
    userServiceSpy.deleteAccount.and.returnValue(of(mockResponse));
    localStorage.setItem('foo', 'bar');

    // Act
    component.deleteAccount();

    // Assert
    expect(userServiceSpy.deleteAccount).toHaveBeenCalled();
    expect(snackBarSpy.success).toHaveBeenCalledWith('Cuenta eliminada');
    expect(localStorage.getItem('foo')).toBeNull();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('deleteAccount() debe notificar el error si la respuesta del servidor no es exitosa', () => {
    // Arrange
    const mockResponse: ApiResponse = { success: false, message: 'No se pudo eliminar la cuenta', data: null };
    userServiceSpy.deleteAccount.and.returnValue(of(mockResponse));

    // Act
    component.deleteAccount();

    // Assert
    expect(snackBarSpy.error).toHaveBeenCalledWith('No se pudo eliminar la cuenta');
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });
});
