import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { RegisterForm } from './register-form';
import { AuthService } from '../../services/auth.service';
import { RegisterResponse } from '../../models/register.model';
import { ApiResponse } from '../../../../core/models/api-response.model';

describe('RegisterForm', () => {
  let component: RegisterForm;
  let fixture: ComponentFixture<RegisterForm>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  const validValue = {
    fullName: 'Test User',
    email: 'user@test.com',
    phone: '',
    password: '123456',
    confirmPassword: '123456'
  };

  beforeEach(async () => {
    // Arrange
    authServiceSpy = jasmine.createSpyObj('AuthService', ['register']);

    await TestBed.configureTestingModule({
      imports: [RegisterForm, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterForm);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  afterEach(() => localStorage.clear());

  it('should create', () => {
    // Assert
    expect(component).toBeTruthy();
  });

  it('debe inicializar el formulario de registro vacío e inválido', () => {
    // Assert
    expect(component.registerForm.invalid).toBeTrue();
  });

  it('debe marcar el formulario como inválido si las contraseñas no coinciden', () => {
    // Arrange
    // Act
    component.registerForm.setValue({ ...validValue, confirmPassword: 'distinta' });

    // Assert
    expect(component.registerForm.hasError('mismatch')).toBeTrue();
  });

  it('no debe llamar al AuthService y debe marcar los campos como tocados si el formulario es inválido', () => {
    // Act
    component.onFormSubmit();

    // Assert
    expect(authServiceSpy.register).not.toHaveBeenCalled();
    expect(component.fullName?.touched).toBeTrue();
  });

  it('debe llamar a AuthService.register(), guardar datos en localStorage y navegar a registration-success cuando es exitoso', () => {
    // Arrange
    const mockResponse: ApiResponse<RegisterResponse> = {
      success: true,
      message: 'ok',
      data: { requiresVerification: true, email: validValue.email, emailSent: true }
    };
    authServiceSpy.register.and.returnValue(of(mockResponse));
    component.registerForm.setValue(validValue);

    // Act
    component.onFormSubmit();

    // Assert
    expect(authServiceSpy.register).toHaveBeenCalledWith(jasmine.objectContaining({ email: validValue.email, fullName: validValue.fullName }));
    expect(localStorage.getItem('registrationEmail')).toBe(validValue.email);
    expect(localStorage.getItem('emailSent')).toBe('true');
    expect(router.navigate).toHaveBeenCalledWith(['/registration-success']);
    expect(component.isLoading).toBeFalse();
  });

  it('debe mostrar el mensaje de error del backend cuando el registro falla', () => {
    // Arrange
    authServiceSpy.register.and.returnValue(throwError(() => ({ error: { message: 'El email ya está registrado' } })));
    component.registerForm.setValue(validValue);

    // Act
    component.onFormSubmit();

    // Assert
    expect(component.errorMessage).toBe('El email ya está registrado');
    expect(component.isLoading).toBeFalse();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
