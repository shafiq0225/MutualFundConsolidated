// src/app/shared/components/holiday-banner/holiday-banner.component.ts
import {
  Component, Input, ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HolidayStatusDto } from '../../../core/models/holiday-status.model';

@Component({
  selector: 'app-holiday-banner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './holiday-banner.component.html',
  styleUrls: ['./holiday-banner.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HolidayBannerComponent {
  @Input() status!: HolidayStatusDto;
  dismissed = false;

  dismiss(): void {
    this.dismissed = true;
  }
}
