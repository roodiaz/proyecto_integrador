import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Longitud mínima de contraseña, debe coincidir con `PasswordPolicy.MinLength` del backend. */
export const PASSWORD_MIN_LENGTH = 8;

/** Longitud máxima de contraseña, debe coincidir con `PasswordPolicy.MaxLength` del backend. */
export const PASSWORD_MAX_LENGTH = 64;

/** Caracteres especiales aceptados, debe coincidir con `PasswordPolicy.AllowedSpecialCharacters` del backend. */
export const PASSWORD_SPECIAL_CHARACTERS = '!@#$%^&*()-_+=?.,;:/';

const SPECIAL_CHAR_REGEX = /[!@#$%^&*()\-_+=?.,;:/]/;

/** Expresión regular completa de la política de contraseñas, equivalente a `PasswordPolicy` del backend. */
export const PASSWORD_POLICY_REGEX =
  /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()\-_+=?.,;:/])[A-Za-z\d!@#$%^&*()\-_+=?.,;:/]{8,64}$/;

/** Un requisito individual de la política de contraseñas, usado por el checklist visual. */
export interface PasswordRequirement {
  /** Sufijo de la clave i18n bajo `PASSWORD_POLICY.REQUIREMENTS`. */
  key: string;
  /** Evalúa si el valor actual cumple este requisito. */
  test: (value: string) => boolean;
}

/** Lista de requisitos de la política de contraseñas, en el orden en que se muestran al usuario. */
export const PASSWORD_REQUIREMENTS: PasswordRequirement[] = [
  { key: 'MIN_LENGTH', test: value => value.length >= PASSWORD_MIN_LENGTH && value.length <= PASSWORD_MAX_LENGTH },
  { key: 'UPPERCASE', test: value => /[A-Z]/.test(value) },
  { key: 'LOWERCASE', test: value => /[a-z]/.test(value) },
  { key: 'NUMBER', test: value => /\d/.test(value) },
  { key: 'SPECIAL', test: value => SPECIAL_CHAR_REGEX.test(value) }
];

/**
 * Validador de Angular que verifica que la contraseña cumpla con la política
 * de seguridad de InvestLab (longitud y combinación de caracteres requeridos).
 *
 * Las cadenas vacías se consideran válidas para este validador: el requisito
 * de "campo requerido" debe cubrirse con `Validators.required`.
 * @returns `{ weakPassword: true }` si la contraseña no cumple la política, o `null` si es válida o está vacía.
 */
export function strongPasswordValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string;

    if (!value) return null;

    return PASSWORD_POLICY_REGEX.test(value) ? null : { weakPassword: true };
  };
}
