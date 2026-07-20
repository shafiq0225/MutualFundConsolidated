import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface DonutSegment {
  value: number;   // absolute value, proportions computed automatically
  color: string;   // CSS color
}

@Component({
  selector: 'app-donut-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="donut" [style.width.px]="size" [style.height.px]="size"
         [style.background]="gradient()">
      <div class="donut-hole" [style.inset.px]="thickness">
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .donut {
      border-radius: 50%;
      position: relative;
      flex-shrink: 0;
    }
    .donut-hole {
      position: absolute;
      background: var(--paper, #fff);
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-direction: column;
    }
  `]
})
export class DonutChartComponent {
  @Input() segments: DonutSegment[] = [];
  @Input() size = 96;
  @Input() thickness = 14;
  @Input() emptyColor = '#EAE3D5';

  gradient = computed(() => {
    const total = this.segments.reduce((s, seg) => s + Math.max(seg.value, 0), 0);

    if (total <= 0) {
      return `conic-gradient(${this.emptyColor} 0deg 360deg)`;
    }

    let acc = 0;
    const stops: string[] = [];
    for (const seg of this.segments) {
      const v = Math.max(seg.value, 0);
      const start = (acc / total) * 360;
      acc += v;
      const end = (acc / total) * 360;
      stops.push(`${seg.color} ${start}deg ${end}deg`);
    }
    return `conic-gradient(${stops.join(', ')})`;
  });
}
