import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaterialModule } from '../../../../shared/material.module';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { FormsModule } from '@angular/forms';
import { VerifyRequest } from '../../models/register.model';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-registration-success',
  standalone: true,
  imports: [
    CommonModule,
    MaterialModule,
    RouterModule,
    FormsModule
  ],
  templateUrl: './registration-success.html',
  styleUrl: './registration-success.css'
})
export class RegistrationSuccess implements OnInit, OnDestroy {
  userEmail: string = '';
  emailSent: boolean = true;
  remainingTime: number = 15 * 60; // 15 minutes in seconds
  timeDisplay: string = '';
  verificationCode = '';
  private intervalId: any;

  constructor(
    private router: Router,
    private authService: AuthService,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    // Get email from localStorage or route params
    const storedEmail = localStorage.getItem('registrationEmail');
    this.userEmail = storedEmail || 'tu correo electrónico';

    const storedEmailSent = localStorage.getItem('emailSent');
    this.emailSent = storedEmailSent === 'true';

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
    this.authService.resendCode(this.userEmail)
      .subscribe({
        next: (response) => {

          if (response.success) {

            this.emailSent = response.data!.emailSent;

            if (response.data!.emailSent) {

              this.remainingTime = 15 * 60;

              if (this.intervalId) {
                clearInterval(this.intervalId);
              }

              this.startCountdown();
            }
          }
        },
        error: (error) => {
          console.error('Error reenviando código', error);
        }
      });
  }

  verifyCode(): void {

    const request = {
      email: this.userEmail,
      code: this.verificationCode
    };

    this.authService.verifyCode(request)
      .subscribe({
        next: (response) => {

          if (response.success) {

            this.snackBar.open(
              response.message,
              'Cerrar',
              { duration: 4000 }
            );

            setTimeout(() => {
              this.router.navigate(['/login']);
            }, 1500);

            this.router.navigate(['/login']);
          }
        },
        error: (error) => {
          this.snackBar.open(
            error.error?.message ?? 'Ocurrió un error',
            'Cerrar',
            {
              duration: 4000
            }
          );
        }
      });
  }
}
