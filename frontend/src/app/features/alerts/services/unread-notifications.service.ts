import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, shareReplay, switchMap } from 'rxjs';
import { NotificationService } from './notification.service';

/**
 * Servicio compartido que centraliza la cantidad de notificaciones sin leer
 * para que cualquier sección de InvestLab (Sidebar, pantalla Alertas, pestaña
 * Notificaciones) pueda mostrarla sin duplicar la consulta al backend.
 *
 * Expone un único stream cacheado que se vuelve a consultar cada vez que se
 * invoca `refresh()`, lo que permite mantener el contador sincronizado luego
 * de generar, leer, eliminar o limpiar notificaciones.
 */
@Injectable({ providedIn: 'root' })
export class UnreadNotificationsService {
  private readonly refreshTrigger$ = new BehaviorSubject<void>(undefined);

  private readonly count$: Observable<number> = this.refreshTrigger$.pipe(
    switchMap(() => this.notificationService.getUnreadCount()),
    map(response => response.data?.count ?? 0),
    shareReplay({ bufferSize: 1, refCount: false })
  );

  constructor(private notificationService: NotificationService) {}

  /** @returns Un observable con la cantidad de notificaciones sin leer, compartido entre todos los suscriptores. */
  watchCount(): Observable<number> {
    return this.count$;
  }

  /** Vuelve a consultar la cantidad de notificaciones sin leer, propagando el nuevo valor a todos los suscriptores. */
  refresh(): void {
    this.refreshTrigger$.next();
  }
}
