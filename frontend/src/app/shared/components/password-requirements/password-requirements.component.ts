import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { PASSWORD_REQUIREMENTS } from '../../validators/password-policy.validator';

/**
 * Checklist visual de la política de contraseñas de InvestLab. Muestra cada
 * requisito (longitud, mayúscula, minúscula, número, carácter especial) y
 * actualiza su estado dinámicamente a medida que el usuario escribe.
 */
@Component({
  selector: 'app-password-requirements',
  standalone: true,
  imports: [CommonModule, MatIconModule, TranslateModule],
  template: `
    <ul class="password-requirements">
      @for (requirement of requirements; track requirement.key) {
        <li [class.met]="requirement.test(password)">
          <mat-icon>{{ requirement.test(password) ? 'check_circle' : 'radio_button_unchecked' }}</mat-icon>
          <span>{{ 'PASSWORD_POLICY.REQUIREMENTS.' + requirement.key | translate }}</span>
        </li>
      }
    </ul>
  `,
  styles: [`
    .password-requirements {
      list-style: none;
      margin: 4px 0 8px;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .password-requirements li {
      display: flex;
      align-items: flex-start;
      gap: 6px;
      font-size: 12px;
      color: var(--il-text-muted);
      transition: color 0.2s;
    }

    .password-requirements li mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
      line-height: 16px;
      flex-shrink: 0;
      margin-top: 1px;
    }

    .password-requirements li.met {
      color: var(--il-success);
    }
  `]
})
export class PasswordRequirementsComponent {
  /** Valor actual del campo de contraseña, evaluado contra cada requisito. */
  @Input() password = '';

  readonly requirements = PASSWORD_REQUIREMENTS;
}
