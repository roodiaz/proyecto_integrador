import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLink, RouterLinkActive } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
  selector: 'app-landing-body',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterLink,
    RouterLinkActive,
    MatCardModule,
    MatButtonModule,
    MatDividerModule,
    MatToolbarModule
  ],
  templateUrl: './landing-body.html',
  styleUrl: './landing-body.css'
})
export class LandingBody {
  activeButton: string | null = null;

  setActive(buttonName: string) {
    this.activeButton = buttonName;
  }

  isActive(buttonName: string): boolean {
    return this.activeButton === buttonName;
  }
}
