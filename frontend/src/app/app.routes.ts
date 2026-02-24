import { Routes } from '@angular/router';
import { LandingBody } from './features/home/components/landing-body/landing-body';
import { LoginForm } from './features/auth/components/login-form/login-form';
import { RegisterForm } from './features/auth/components/register-form/register-form';
import { AboutPage } from './features/home/components/about-page/about-page';
import { ContactForm } from './features/home/components/contact-form/contact-form';
import { Dashboard } from './features/dashboard/components/dashboard/dashboard';
import { UserProfile } from './features/settings/components/user-profile/user-profile';
import { Alerts } from './features/alerts/components/alerts/alerts';
import { LayoutComponent } from './layout/layout';

export const routes: Routes = [
  // rutas publicas
  {
    path: '',
    component: LandingBody,
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login', component: LoginForm },
      { path: 'registro', component: RegisterForm },
      { path: 'nosotros', component: AboutPage },
      { path: 'contacto', component: ContactForm },
    ],
  },
  
  // rutas del privadas
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'perfil', component: UserProfile },
      { path: 'alertas', component: Alerts },
    ]
  },
  
  // redireccion de rutas desconocidas
  { path: '**', redirectTo: '' },
];
