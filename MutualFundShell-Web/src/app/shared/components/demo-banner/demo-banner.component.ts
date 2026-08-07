import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-demo-banner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './demo-banner.component.html',
  styleUrl: './demo-banner.component.scss'
})
export class DemoBannerComponent {
  constructor(public authService: AuthService) {}
}
