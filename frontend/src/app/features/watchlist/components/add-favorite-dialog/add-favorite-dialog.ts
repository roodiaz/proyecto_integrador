import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../../shared/material.module';
import { FormsModule } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
    selector: 'app-add-favorite-dialog',
    standalone: true,
    imports: [
        CommonModule,
        MaterialModule,
        FormsModule
    ],
    templateUrl: './add-favorite-dialog.html',
    styleUrl: './add-favorite-dialog.css'
})
export class AddFavoriteDialog {

    constructor(
        private dialogRef: MatDialogRef<AddFavoriteDialog>
    ) { }

    searchTerm = '';

    popularAssets = [
        { symbol: 'AAPL', name: 'Apple Inc.' },
        { symbol: 'MSFT', name: 'Microsoft Corporation' },
        { symbol: 'NVDA', name: 'NVIDIA Corporation' },
        { symbol: 'TSLA', name: 'Tesla Inc.' }
    ];

    add(symbol: string): void {
        this.dialogRef.close(symbol);
    }
}