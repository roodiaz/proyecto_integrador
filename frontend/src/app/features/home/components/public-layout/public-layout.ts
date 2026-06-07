import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MaterialModule } from '../../../../shared/material.module';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterModule, MaterialModule],
  templateUrl: './public-layout.html',
  styleUrl: './public-layout.css'
})
export class PublicLayout {}
