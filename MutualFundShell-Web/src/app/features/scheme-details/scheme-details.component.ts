import {
  Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef,
  ElementRef, ViewChild, ViewChildren,
  QueryList, AfterViewInit, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Chart, registerables } from 'chart.js';

import { SchemeDetailsService } from '../../core/services/scheme-details.service';
import { SchemeDetailsDto, PeriodReturnDto } from '../../core/models/scheme-details.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

export interface PeriodCard {
  label: string;
  period: PeriodReturnDto | null;
  sliceCount: number;
}

@Component({
  selector: 'app-scheme-details',
  standalone: true,
  imports: [CommonModule, LoadingSpinnerComponent],
  templateUrl: './scheme-details.component.html',
  styleUrls: ['./scheme-details.component.scss']
  ,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SchemeDetailsComponent implements OnInit, AfterViewInit, OnDestroy {
  scheme: SchemeDetailsDto | null = null;
  loading = true;
  schemeCode = '';

  @ViewChild('sparklineCanvas')
  canvasRef!: ElementRef<HTMLCanvasElement>;

  @ViewChildren('periodCanvas')
  periodCanvases!: QueryList<ElementRef<HTMLCanvasElement>>;

  sparklineChart?: Chart<'line', number[], string>;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private service: SchemeDetailsService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.schemeCode =
      this.route.snapshot.paramMap.get('schemeCode') || '';
    this.loadDetails();
  }

  ngOnDestroy(): void {
    this.sparklineChart?.destroy();
  }

  // trackBy for period cards to avoid unnecessary re-renders
  trackByCard(_index: number, item: PeriodCard): string {
    return item.label;
  }

  ngAfterViewInit(): void {
    if (this.scheme?.navHistory?.length) {
      this.drawAllCharts();
    }
  }

  loadDetails(): void {
    this.loading = true;
    this.service.getSchemeDetails(this.schemeCode).subscribe({
      next: (data) => {
        this.scheme = data;
        this.loading = false;
        this.cdr.detectChanges();
        setTimeout(() => this.drawAllCharts(), 120);
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load scheme details.');
        this.cdr.detectChanges();
      }
    });
  }

  // ── Period card definitions ──────────────────────────────────────
  get periodCards(): PeriodCard[] {
    if (!this.scheme) return [];
    return [
      { label: '1 Month', period: this.scheme.oneMonth, sliceCount: 5 },
      { label: '3 Month', period: this.scheme.threeMonth, sliceCount: 10 },
      { label: '6 Month', period: this.scheme.sixMonth, sliceCount: 18 },
      { label: '1 Year', period: this.scheme.oneYear, sliceCount: 25 },
      { label: '3 Year', period: this.scheme.threeYear, sliceCount: 30 },
    ];
  }

  get hasPeriodReturns(): boolean {
    return this.periodCards.some(card => card.period?.hasData === true);
  }

  // ── Draw all charts ──────────────────────────────────────────────
  drawAllCharts(): void {
    this.drawSparkline();
    setTimeout(() => this.drawPeriodSparklines(), 60);
  }

  drawSparkline(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas || !this.scheme?.navHistory?.length) return;
    this.sparklineChart?.destroy();

    Chart.register(...registerables);

    const labels = this.scheme.navHistory.map(point => point.dateText);
    const data = this.scheme.navHistory.map(point => point.nav);

    this.sparklineChart = new Chart(canvas, {
      type: 'line',
      data: {
        labels,
        datasets: [{
          label: 'NAV',
          data,
          borderColor: this.scheme.isDailyUp ? '#2F6F62' : '#9C3B26',
          backgroundColor: this.scheme.isDailyUp ? 'rgba(47,111,98,0.15)' : 'rgba(156,59,38,0.15)',
          fill: true,
          tension: 0.25,
          pointRadius: 0,
          borderWidth: 2
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            enabled: true,
            mode: 'index',
            intersect: false,
            callbacks: {
              label: (context) => `NAV: ₹${context.parsed.y}`
            }
          }
        },
        interaction: {
          mode: 'index',
          intersect: false
        },
        scales: {
          x: {
            display: true,
            title: { display: true, text: 'Date' },
            ticks: { autoSkip: true, maxTicksLimit: 6 }
          },
          y: {
            display: true,
            title: { display: true, text: 'NAV' },
            ticks: {
              callback: function(value: any) { return `₹${value}`; }
            }
          }
        }
      }
    });
  }

  drawPeriodSparklines(): void {
    if (!this.periodCanvases || !this.scheme?.navHistory?.length) return;

    // FIX: #periodCanvas lives inside *ngIf="card.period?.hasData", so
    // QueryList only holds canvases for cards where hasData === true.
    // Build a parallel array of only the data-bearing cards so that
    // canvases[i] always corresponds to cardsWithData[i].
    const cardsWithData = this.periodCards.filter(c => c.period?.hasData);

    this.periodCanvases.toArray().forEach((ref, i) => {
      const card = cardsWithData[i];
      const canvas = ref?.nativeElement;
      if (!canvas || !card?.period?.hasData) return;

      const slice = this.scheme!.navHistory
        .slice(-card.sliceCount)
        .map(d => d.nav);

      this.drawChart(canvas, slice, card.period.isPositive, 52);
    });
  }

  private drawChart(
    canvas: HTMLCanvasElement,
    navs: number[],
    isUp: boolean,
    height: number
  ): void {
    const ctx = canvas.getContext('2d');
    if (!ctx || navs.length < 2) return;

    // FIX: getBoundingClientRect().width is reliable on mobile where
    // offsetWidth can return 0 before the first paint completes.
    const W = Math.round(
      canvas.getBoundingClientRect().width || canvas.offsetWidth || 200
    );
    const H = height;
    canvas.width = W;
    canvas.height = H;

    const min = Math.min(...navs);
    const max = Math.max(...navs);
    const range = max - min || 1;
    const pad = { t: 6, r: 4, b: 6, l: 4 };
    const cW = W - pad.l - pad.r;
    const cH = H - pad.t - pad.b;
    const step = cW / (navs.length - 1);

    const pts = navs.map((n, i) => ({
      x: pad.l + i * step,
      y: pad.t + cH - ((n - min) / range) * cH
    }));

    const color = isUp ? '#2F6F62' : '#9C3B26';
    const grad = ctx.createLinearGradient(0, 0, 0, H);
    grad.addColorStop(0, isUp
      ? 'rgba(47,111,98,0.22)'
      : 'rgba(156,59,38,0.22)');
    grad.addColorStop(1, 'rgba(0,0,0,0)');

    // Fill
    ctx.beginPath();
    ctx.moveTo(pts[0].x, pts[0].y);
    pts.forEach(p => ctx.lineTo(p.x, p.y));
    ctx.lineTo(pts[pts.length - 1].x, H);
    ctx.lineTo(pts[0].x, H);
    ctx.closePath();
    ctx.fillStyle = grad;
    ctx.fill();

    // Line
    ctx.beginPath();
    ctx.moveTo(pts[0].x, pts[0].y);
    pts.forEach(p => ctx.lineTo(p.x, p.y));
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.5;
    ctx.lineJoin = 'round';
    ctx.stroke();

  }

  copySchemeCode(): void {
    const code = this.scheme?.schemeCode || this.schemeCode || '';
    if (!code) return;
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(code).then(() => {
        this.toastr.success('Scheme code copied to clipboard');
      }, () => {
        this.toastr.success('Copied');
      });
    } else {
      // fallback
      const el = document.createElement('textarea');
      el.value = code;
      el.style.position = 'fixed'; el.style.left = '-9999px';
      document.body.appendChild(el);
      el.select();
      try { document.execCommand('copy'); this.toastr.success('Scheme code copied'); } catch { }
      document.body.removeChild(el);
    }
  }

  goBack(): void {
    this.router.navigate(['/nav']);
  }
}
