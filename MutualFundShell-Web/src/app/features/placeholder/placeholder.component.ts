import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'shell-placeholder',
  standalone: true,
  template: `
    <div class="placeholder-page">
      <i class="fas fa-drafting-compass"></i>
      <h2>{{ title }}</h2>
      <p>This section isn't wired up yet — it's reserved in the sidebar for a future micro frontend.</p>
    </div>
  `,
  styles: [`
    .placeholder-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      gap: 12px;
      padding: 80px 24px;
      color: var(--text-muted, #6B6455);
    }
    .placeholder-page i { font-size: 32px; color: var(--gold, #C08A2E); }
    .placeholder-page h2 {
      font-family: 'Fraunces', Georgia, serif;
      color: var(--text-primary, #1B2A4A);
      margin: 0;
    }
    .placeholder-page p { margin: 0; max-width: 420px; }
  `]
})
export class PlaceholderComponent {
  private readonly route = inject(ActivatedRoute);
  title = this.route.snapshot.data['title'] ?? 'Coming soon';
}
