import { Routes } from '@angular/router';
import { LandingBody } from './features/home/components/landing-body/landing-body';
import { LoginForm } from './features/auth/components/login-form/login-form';
import { RegisterForm } from './features/auth/components/register-form/register-form';
import { RegistrationSuccess } from './features/auth/components/registration-success/registration-success';
import { AboutPage } from './features/home/components/about-page/about-page';
import { ContactForm } from './features/home/components/contact-form/contact-form';
import { Dashboard } from './features/dashboard/components/dashboard/dashboard';
import { UserProfile } from './features/settings/components/user-profile/user-profile';
import { Alerts } from './features/alerts/components/alerts/alerts';
import { Portfolio } from './features/portfolio/components/portfolio/portfolio';
import { Watchlist } from './features/watchlist/components/watchlist/watchlist';
import { Market } from './features/market/components/market/market';
import { LayoutComponent } from './layout/layout';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // rutas publicas
  {
    path: '',
    component: LandingBody,
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login', component: LoginForm },
      { path: 'registro', component: RegisterForm },
      { path: 'registration-success', component: RegistrationSuccess },
      { path: 'nosotros', component: AboutPage },
      { path: 'contacto', component: ContactForm },
    ],
  },
  
  // rutas del privadas
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
  
  // redireccion de rutas desconocidas
  { path: '**', redirectTo: '' },
];
