import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';

import { Watchlist } from './watchlist';
import { WatchlistService } from '../../services/watchlist.service';
import { SnackBarService } from '../../../../core/services/snackbar.service';
import { FavoriteListResponse } from '../../models/watchlist-item';
import { ApiResponse } from '../../../../core/models/api-response.model';

describe('Watchlist', () => {
  let component: Watchlist;
  let fixture: ComponentFixture<Watchlist>;
  let watchlistServiceSpy: jasmine.SpyObj<WatchlistService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let dialogOpenSpy: jasmine.Spy;
  let snackBarSpy: jasmine.SpyObj<SnackBarService>;

  const mockListResponse: FavoriteListResponse = {
    success: true,
    message: 'ok',
    data: {
      items: [
        { id: 1, symbol: 'AAPL', name: 'Apple', price: 150, variationPercent: 1.5 },
        { id: 2, symbol: 'TSLA', name: 'Tesla', price: 200, variationPercent: -2 }
      ],
      total: 2,
      page: 1,
      pageSize: 7,
      currentFavorites: 2,
      maxFavorites: 10
    }
  };

  beforeEach(async () => {
    // Arrange
    watchlistServiceSpy = jasmine.createSpyObj('WatchlistService', ['getFavorites', 'addFavorite', 'removeFavorite']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    snackBarSpy = jasmine.createSpyObj('SnackBarService', ['success', 'error']);

    watchlistServiceSpy.getFavorites.and.returnValue(of(mockListResponse));

    await TestBed.configureTestingModule({
      imports: [Watchlist],
      providers: [
        { provide: WatchlistService, useValue: watchlistServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: SnackBarService, useValue: snackBarSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Watchlist);
    component = fixture.componentInstance;
    dialogOpenSpy = spyOn(fixture.debugElement.injector.get(MatDialog), 'open');
    fixture.detectChanges();
  });

  it('should create', () => {
    // Assert
    expect(component).toBeTruthy();
  });

  it('debe cargar la watchlist al inicializar el componente', () => {
    // Assert
    expect(watchlistServiceSpy.getFavorites).toHaveBeenCalledWith({ page: 1, pageSize: 7, search: '' });
    expect(component.watchlistItems.length).toBe(2);
    expect(component.totalRecords).toBe(2);
    expect(component.currentFavorites).toBe(2);
    expect(component.maxFavorites).toBe(10);
  });

  it('debe vaciar la lista si la respuesta no es exitosa', () => {
    // Arrange
    watchlistServiceSpy.getFavorites.and.returnValue(of({ success: false, message: 'error', data: null } as FavoriteListResponse));

    // Act
    component.loadWatchlist();

    // Assert
    expect(component.watchlistItems).toEqual([]);
    expect(component.totalRecords).toBe(0);
  });

  it('debe vaciar la lista si la petición falla', () => {
    // Arrange
    watchlistServiceSpy.getFavorites.and.returnValue(throwError(() => new Error('network error')));

    // Act
    component.loadWatchlist();

    // Assert
    expect(component.watchlistItems).toEqual([]);
  });

  it('onSearch() debe filtrar los items por símbolo o nombre', () => {
    // Arrange
    component.searchTerm = 'tsla';

    // Act
    component.onSearch();

    // Assert
    expect(component.filteredItems.length).toBe(1);
    expect(component.filteredItems[0].symbol).toBe('TSLA');
  });

  it('clearSearch() debe limpiar el término de búsqueda y mostrar todos los items', () => {
    // Arrange
    component.searchTerm = 'tsla';
    component.onSearch();

    // Act
    component.clearSearch();

    // Assert
    expect(component.searchTerm).toBe('');
    expect(component.filteredItems.length).toBe(2);
  });

  it('removeFromWatchlist() debe llamar al service, notificar el éxito y recargar la lista', () => {
    // Arrange
    watchlistServiceSpy.removeFavorite.and.returnValue(of({ success: true, message: 'Favorito eliminado', data: null } as ApiResponse));
    watchlistServiceSpy.getFavorites.calls.reset();

    // Act
    component.removeFromWatchlist('AAPL');

    // Assert
    expect(watchlistServiceSpy.removeFavorite).toHaveBeenCalledWith('AAPL');
    expect(snackBarSpy.success).toHaveBeenCalled();
    expect(watchlistServiceSpy.getFavorites).toHaveBeenCalled();
  });

  it('removeFromWatchlist() debe notificar el error si la operación falla', () => {
    // Arrange
    watchlistServiceSpy.removeFavorite.and.returnValue(throwError(() => ({ error: { message: 'No se pudo eliminar' } })));

    // Act
    component.removeFromWatchlist('AAPL');

    // Assert
    expect(snackBarSpy.error).toHaveBeenCalledWith('No se pudo eliminar');
  });

  it('openAddFavoriteDialog() debe agregar el favorito y recargar la lista cuando el diálogo devuelve un símbolo', () => {
    // Arrange
    const afterClosed$ = of('MSFT');
    dialogOpenSpy.and.returnValue({ afterClosed: () => afterClosed$ } as any);
    watchlistServiceSpy.addFavorite.and.returnValue(of({ success: true, message: 'Favorito agregado', data: null } as ApiResponse));
    watchlistServiceSpy.getFavorites.calls.reset();

    // Act
    component.openAddFavoriteDialog();

    // Assert
    expect(watchlistServiceSpy.addFavorite).toHaveBeenCalledWith('MSFT');
    expect(snackBarSpy.success).toHaveBeenCalled();
    expect(watchlistServiceSpy.getFavorites).toHaveBeenCalled();
  });

  it('openAddFavoriteDialog() no debe agregar nada si el diálogo se cierra sin símbolo', () => {
    // Arrange
    dialogOpenSpy.and.returnValue({ afterClosed: () => of(undefined) } as any);

    // Act
    component.openAddFavoriteDialog();

    // Assert
    expect(watchlistServiceSpy.addFavorite).not.toHaveBeenCalled();
  });

  it('goToMarket() debe navegar a /market con el ticker como query param', () => {
    // Act
    component.goToMarket('AAPL');

    // Assert
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/market'], { queryParams: { ticker: 'AAPL' } });
  });

  it('goToAlerts() debe navegar a /alerts con el ticker como query param', () => {
    // Act
    component.goToAlerts('AAPL');

    // Assert
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/alerts'], { queryParams: { ticker: 'AAPL' } });
  });
});
