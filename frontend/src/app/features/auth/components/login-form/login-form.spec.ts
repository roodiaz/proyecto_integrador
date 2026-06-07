import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { LoginForm } from './login-form';
import { AuthService } from '../../services/auth.service';
import { LoginResponse } from '../../models/login.model';

describe('LoginForm', () => {
  let component: LoginForm;
  let fixture: ComponentFixture<LoginForm>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    // Arrange
    authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);

    await TestBed.configureTestingModule({
      imports: [LoginForm, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginForm);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  afterEach(() => sessionStorage.clear());

  it('should create', () => {
    // Assert
    expect(component).toBeTruthy();
  });

  it('debe inicializar el formulario con los campos email y password vacíos e inválidos', () => {
    // Assert
    expect(component.loginForm.value).toEqual({ email: '', password: '' });
    expect(component.loginForm.invalid).toBeTrue();
  });

  it('no debe llamar al AuthService si el formulario es inválido', () => {
    // Arrange
    component.loginForm.setValue({ email: 'invalido', password: '' });

    // Act
    component.onSubmit();

    // Assert
    expect(authServiceSpy.login).not.toHaveBeenCalled();
  });

  it('debe llamar a AuthService.login(), guardar los tokens y navegar al dashboard si el login es exitoso', () => {
    // Arrange
    const mockResponse: LoginResponse = { success: true, message: 'ok', data: { tokens: { accessToken: 'access-1', refreshToken: 'refresh-1' } } };
    authServiceSpy.login.and.returnValue(of(mockResponse));
    component.loginForm.setValue({ email: 'user@test.com', password: '123456' });

    // Act
    component.onSubmit();

    // Assert
    expect(authServiceSpy.login).toHaveBeenCalledWith({ email: 'user@test.com', password: '123456' });
    expect(sessionStorage.getItem('accessToken')).toBe('access-1');
    expect(sessionStorage.getItem('refreshToken')).toBe('refresh-1');
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('debe mostrar el mensaje de error cuando la respuesta no es exitosa', () => {
    // Arrange
    const mockResponse: LoginResponse = { success: false, message: 'Credenciales inválidas', data: null };
    authServiceSpy.login.and.returnValue(of(mockResponse));
    component.loginForm.setValue({ email: 'user@test.com', password: '123456' });

    // Act
    component.onSubmit();

    // Assert
    expect(component.loginError).toBe('Credenciales inválidas');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('debe mostrar el mensaje de error del backend cuando la petición falla', () => {
    // Arrange
    authServiceSpy.login.and.returnValue(throwError(() => ({ error: { message: 'Error de servidor' } })));
    component.loginForm.setValue({ email: 'user@test.com', password: '123456' });

    // Act
    component.onSubmit();

    // Assert
    expect(component.loginError).toBe('Error de servidor');
  });
});
