import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="spinner-wrap" [class.overlay]="overlay">
      <div class="spinner" [class]="'spinner--' + size">
        <i class="fas fa-circle-notch fa-spin"></i>
      </div>
      <p *ngIf="message" class="spinner-msg">{{ message }}</p>
    </div>
  `,
  styles: [`
    .spinner-wrap {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      padding: 24px;
    }
    .overlay {
      position: fixed;
      inset: 0;
      background: rgba(255,255,255,0.8);
      z-index: 9999;
    }
    .spinner { color: #1F4E79; }
    .spinner--sm { font-size: 20px; }
    .spinner--md { font-size: 32px; }
    .spinner--lg { font-size: 48px; }
    .spinner-msg { color: #5D6D7E; font-size: 14px; }
  `]
})
export class LoadingSpinnerComponent {
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() message?: string;
  @Input() overlay: boolean = false;
}
