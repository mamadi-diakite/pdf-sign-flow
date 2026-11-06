import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@pdf-sign-flow/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { RegisterPage } from './auth/register-page';
import { HomePage } from './home/home-page';
import { NewRequestPage } from './requests/new-request-page';
import { RequestDetailsPage } from './requests/request-details-page';
import { RequestsPage } from './requests/requests-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'PDF Sign Flow' },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [guestGuard],
    title: 'Log in · PDF Sign Flow',
  },
  {
    path: 'register',
    component: RegisterPage,
    canActivate: [guestGuard],
    title: 'Create an account · PDF Sign Flow',
  },
  {
    path: 'account/password',
    component: ChangePasswordPage,
    canActivate: [authGuard],
    title: 'Change password · PDF Sign Flow',
  },
  {
    path: 'requests',
    component: RequestsPage,
    canActivate: [authGuard],
    title: 'My requests · PDF Sign Flow',
  },
  {
    path: 'requests/new',
    component: NewRequestPage,
    canActivate: [authGuard],
    title: 'New request · PDF Sign Flow',
  },
  {
    path: 'requests/:id',
    component: RequestDetailsPage,
    canActivate: [authGuard],
    title: 'Request · PDF Sign Flow',
  },
  { path: '**', redirectTo: '' },
];
