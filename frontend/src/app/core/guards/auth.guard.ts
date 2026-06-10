import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

export const authGuard: CanActivateFn = () => {

  const router = inject(Router);

  const accessToken = sessionStorage.getItem('accessToken');
  const refreshToken = sessionStorage.getItem('refreshToken');

  // Si el access token expiró pero todavía hay un refresh token, se permite
  // el acceso: el interceptor renovará la sesión en la primera petición.
  if (accessToken || refreshToken) {
    return true;
  }

  router.navigate(['/login']);

  return false;
};