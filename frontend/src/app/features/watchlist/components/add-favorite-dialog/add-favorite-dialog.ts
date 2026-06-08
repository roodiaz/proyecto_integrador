import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../../shared/material.module';
import { FormsModule } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { InfoTooltipComponent } from '../../../../shared/components/info-tooltip/info-tooltip.component';

/**
 * Diálogo para agregar un activo a la lista de favoritos (watchlist).
 *
 * Permite buscar un activo por símbolo y agregarlo directamente, o elegirlo
 * de una lista de activos populares predefinida. Al confirmar, cierra el
 * diálogo devolviendo el símbolo elegido para que el componente que lo abrió
 * lo agregue a favoritos.
 */
@Component({
    selector: 'app-add-favorite-dialog',
    standalone: true,
    imports: [
        CommonModule,
        MaterialModule,
        FormsModule,
        InfoTooltipComponent
    ],
    templateUrl: './add-favorite-dialog.html',
    styleUrl: './add-favorite-dialog.css'
})
export class AddFavoriteDialog {

    // ── Búsqueda ──
    searchTerm = '';

    // ── Activos populares (sugerencias predefinidas) ──
    popularAssets = [
        { symbol: 'AAPL', name: 'Apple Inc.' },
        { symbol: 'MSFT', name: 'Microsoft Corporation' },
        { symbol: 'NVDA', name: 'NVIDIA Corporation' },
        { symbol: 'TSLA', name: 'Tesla Inc.' }
    ];

    /**
     * @param dialogRef Referencia al diálogo, usada para cerrarlo y devolver el símbolo elegido.
     */
    constructor(
        private dialogRef: MatDialogRef<AddFavoriteDialog>
    ) { }

    /**
     * Cierra el diálogo devolviendo el símbolo del activo elegido, ya sea
     * el ingresado en el buscador o uno seleccionado de la lista de populares.
     * @param symbol Símbolo del activo a agregar a favoritos.
     */
    add(symbol: string): void {
        this.dialogRef.close(symbol);
    }
}
