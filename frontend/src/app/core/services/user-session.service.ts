import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, shareReplay, switchMap } from 'rxjs';
import { UserService } from '../../features/settings/services/user.service';
import { ProfileData } from '../../features/settings/models/user-profile.model';

/**
 * Servicio compartido que centraliza la información básica del usuario
 * autenticado (nombre, email, avatar, último acceso, etc.) para que
 * cualquier sección de InvestLab (Header, Sidebar, pantalla de
 * Configuración) pueda mostrarla sin duplicar la consulta al backend.
 *
 * Expone un único stream cacheado que se vuelve a consultar cada vez que
 * se invoca `refresh()`, lo que permite mantener la información sincronizada
 * luego de actualizar el perfil o la imagen de usuario.
 */
@Injectable({ providedIn: 'root' })
export class UserSessionService {
  private readonly refreshTrigger$ = new BehaviorSubject<void>(undefined);

  private readonly profile$: Observable<ProfileData | null> = this.refreshTrigger$.pipe(
    switchMap(() => this.userService.getProfile()),
    map(response => response.data ?? null),
    shareReplay({ bufferSize: 1, refCount: false })
  );

  constructor(private userService: UserService) {}

  /** @returns Un observable con la información básica del usuario autenticado, compartido entre todos los suscriptores. */
  watchProfile(): Observable<ProfileData | null> {
    return this.profile$;
  }

  /** Vuelve a consultar el perfil del usuario, propagando la nueva información a todos los suscriptores. */
  refresh(): void {
    this.refreshTrigger$.next();
  }
}
