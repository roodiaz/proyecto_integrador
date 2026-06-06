import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-info-tooltip',
  standalone: true,
  imports: [MatIconModule, MatTooltipModule],
  template: `
    <button
      class="info-tooltip-btn"
      type="button"
      [matTooltip]="text"
      matTooltipPosition="above"
      [matTooltipShowDelay]="100"
      aria-label="Más información"
    >
      <mat-icon>help_outline</mat-icon>
    </button>
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
    }

    .info-tooltip-btn {
      background: none;
      border: none;
      cursor: pointer;
      padding: 2px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      color: rgba(255, 255, 255, 0.35);
      border-radius: 50%;
      transition: color 0.2s, background 0.2s;
      line-height: 1;
    }

    .info-tooltip-btn:hover {
      color: rgba(173, 198, 255, 0.85);
      background: rgba(173, 198, 255, 0.1);
    }

    .info-tooltip-btn mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
      line-height: 16px;
    }
  `]
})
export class InfoTooltipComponent {
  @Input() text: string = '';
}
