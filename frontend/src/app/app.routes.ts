import { Routes } from '@angular/router';
import { PublicLayout } from './features/home/components/public-layout/public-layout';
import { LoginForm } from './features/auth/components/login-form/login-form';
import { RegisterForm } from './features/auth/components/register-form/register-form';
import { RegistrationSuccess } from './features/auth/components/registration-success/registration-success';
import { ForgotPasswordForm } from './features/auth/components/forgot-password-form/forgot-password-form';
import { ContactForm } from './features/home/components/contact-form/contact-form';
import { Dashboard } from './features/dashboard/components/dashboard/dashboard';
import { UserProfile } from './features/settings/components/user-profile/user-profile';
import { Alerts } from './features/alerts/components/alerts/alerts';
import { Portfolio } from './features/portfolio/components/portfolio/portfolio';
import { Watchlist } from './features/watchlist/components/watchlist/watchlist';
import { Market } from './features/market/components/market/market';
import { LayoutComponent } from './layout/layout-sidebar';
import { authGuard } from './core/guards/auth.guard';
import { Landing } from './features/home/components/landing/landing';

export const routes: Routes = [

  // Landing
  {
    path: '',
    component: Landing,
    pathMatch: 'full'
  },

  // Públicas
  {
    path: '',
    component: PublicLayout,
    children: [
      { path: 'login', component: LoginForm },
      { path: 'registro', component: RegisterForm },
      { path: 'registration-success', component: RegistrationSuccess },
      { path: 'recuperar-contrasena', component: ForgotPasswordForm },
      { path: 'contacto', component: ContactForm },
    ],
  },

  // Privadas
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
      { path: 'profile', component: UserProfile, canActivate: [authGuard] },
      { path: 'alerts', component: Alerts, canActivate: [authGuard] },
      { path: 'portfolio', component: Portfolio, canActivate: [authGuard] },
      { path: 'watchlist', component: Watchlist, canActivate: [authGuard] },
      { path: 'market', component: Market, canActivate: [authGuard] },
    ]
  },

  { path: '**', redirectTo: '' }
];
