import { Routes } from '@angular/router';
import { LandingBody } from './components/landing-body/landing-body';
import { LoginForm } from './components/login-form/login-form';
import { RegisterForm } from './components/register-form/register-form';
import { AboutPage } from './components/about-page/about-page';
import { ContactForm } from './components/contact-form/contact-form';
import { DashboardComponent } from './components/dashboard/dashboard';

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
  { path: 'dashboard', component: DashboardComponent },
  
  // redireccion de rutas desconocidas
  { path: '**', redirectTo: '' },
];
