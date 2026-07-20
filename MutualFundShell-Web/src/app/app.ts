import { Component, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { SidebarComponent } from './layout/sidebar/sidebar.component';
import { TopbarComponent } from './layout/topbar/topbar.component';
import { LayoutStateService } from './core/services/layout-state.service';

// Routes that render full-bleed with no sidebar/topbar chrome. Written as
// prefix checks so a future full-page auth route (password reset, etc.)
// can opt in the same way — just add its prefix here.
const CHROME_LESS_PREFIXES = ['/login', '/register'];

function isChromeLess(url: string): boolean {
  return CHROME_LESS_PREFIXES.some((prefix) => url.startsWith(prefix));
}

@Component({
  selector: 'shell-root',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class ShellRoot {
  readonly layout = inject(LayoutStateService);
  private readonly router = inject(Router);

  showChrome = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => !isChromeLess(e.urlAfterRedirects)),
      startWith(!isChromeLess(this.router.url))
    ),
    { initialValue: !isChromeLess(this.router.url) }
  );
}
