import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type StatCardColor = 'primary' | 'accent' | 'warning' | 'danger';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stat-card.component.html',
  styleUrls: ['./stat-card.component.scss']
})
export class StatCardComponent {
  @Input() title: string = '';
  @Input() value: string | number = '0';
  @Input() icon: string = 'fa-chart-bar';
  @Input() color: StatCardColor = 'primary';
  @Input() trend?: number;
  @Input() subtitle?: string;
  @Input() loading: boolean = false;

  get trendClass(): string {
    if (!this.trend) return '';
    return this.trend > 0 ? 'trend-up' : 'trend-down';
  }

  get trendIcon(): string {
    if (!this.trend) return '';
    return this.trend > 0 ? 'fa-arrow-trend-up' : 'fa-arrow-trend-down';
  }

  get trendText(): string {
    if (!this.trend) return '';
    return `${this.trend > 0 ? '+' : ''}${this.trend}%`;
  }
}
