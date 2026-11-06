import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ApiHealthService, AuthService, SessionStore } from '@pdf-sign-flow/shared/core';
import { Shell } from '@pdf-sign-flow/shared/ui';

@Component({
  selector: 'psf-root',
  imports: [RouterOutlet, RouterLink, Shell],
  template: `
    <psf-shell appName="PDF Sign Flow" [apiStatus]="apiStatus()">
      <ng-container shellNav>
        @if (user(); as user) {
          <span class="text-slate-600" data-testid="user-name">{{ user.displayName }}</span>
          <a routerLink="/requests" class="text-indigo-600">My requests</a>
          <a routerLink="/account/password" class="text-indigo-600">Change password</a>
          <button type="button" class="text-indigo-600" (click)="logOut()">Log out</button>
        } @else {
          <a routerLink="/login" class="text-indigo-600">Log in</a>
          <a routerLink="/register" class="text-indigo-600">Create an account</a>
        }
      </ng-container>
      <router-outlet />
    </psf-shell>
  `,
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = inject(SessionStore).user;
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });

  protected logOut(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
