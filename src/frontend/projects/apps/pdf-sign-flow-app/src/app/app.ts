import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { ApiHealthService } from '@pdf-sign-flow/shared/core';
import { Shell } from '@pdf-sign-flow/shared/ui';

@Component({
  selector: 'psf-root',
  imports: [RouterOutlet, Shell],
  template: `
    <psf-shell appName="PDF Sign Flow" [apiStatus]="apiStatus()">
      <router-outlet />
    </psf-shell>
  `,
})
export class App {
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });
}
