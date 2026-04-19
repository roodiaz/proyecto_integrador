import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../../shared/material.module';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-registration-success',
  standalone: true,
  imports: [
    CommonModule,
    MaterialModule,
    RouterModule
  ],
  templateUrl: './registration-success.html',
  styleUrl: './registration-success.css'
})
export class RegistrationSuccess implements OnInit, OnDestroy {
  userEmail: string = '';
  remainingTime: number = 15 * 60; // 15 minutes in seconds
  timeDisplay: string = '';
  private intervalId: any;

  constructor(private router: Router) {}

  ngOnInit(): void {
    // Get email from localStorage or route params
    const storedEmail = localStorage.getItem('registrationEmail');
    this.userEmail = storedEmail || 'tu correo electrónico';
    
    // Start countdown timer
    this.startCountdown();
  }

  ngOnDestroy(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }

  private startCountdown(): void {
    this.updateTimeDisplay();
    
    this.intervalId = setInterval(() => {
      this.remainingTime--;
      this.updateTimeDisplay();
      
      if (this.remainingTime <= 0) {
        clearInterval(this.intervalId);
        this.handleTimeExpired();
      }
    }, 1000);
  }

  private updateTimeDisplay(): void {
    const minutes = Math.floor(this.remainingTime / 60);
    const seconds = this.remainingTime % 60;
    this.timeDisplay = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  }

  private handleTimeExpired(): void {
    // Redirect to login page when time expires
    this.router.navigate(['/login']);
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  resendEmail(): void {
    // TODO: Implement resend email functionality
    console.log('Reenviar correo a:', this.userEmail);
    // Reset timer
    this.remainingTime = 15 * 60;
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
    this.startCountdown();
  }
}
