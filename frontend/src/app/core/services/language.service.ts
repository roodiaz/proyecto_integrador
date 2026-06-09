import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject } from 'rxjs';

export type Language = 'es' | 'en';

export const SUPPORTED_LANGUAGES: Language[] = ['es', 'en'];
export const DEFAULT_LANGUAGE: Language = 'es';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {

  private readonly translate = inject(TranslateService);
  private readonly _current$ = new BehaviorSubject<Language>(DEFAULT_LANGUAGE);

  readonly current$ = this._current$.asObservable();

  get current(): Language {
    return this._current$.value;
  }

  private readonly STORAGE_KEY = 'investlab-language';

  initialize(lang?: string | null): void {
    const stored = localStorage.getItem(this.STORAGE_KEY) as Language | null;
    const resolved = this.resolve(lang ?? stored);
    this.translate.use(resolved);
    this._current$.next(resolved);
  }

  setLanguage(lang: Language): void {
    this.translate.use(lang);
    this._current$.next(lang);
    localStorage.setItem(this.STORAGE_KEY, lang);
  }

  instant(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }

  private resolve(lang?: string | null): Language {
    const candidate = (lang ?? '').toLowerCase().trim().split('-')[0];
    return (SUPPORTED_LANGUAGES as string[]).includes(candidate)
      ? (candidate as Language)
      : DEFAULT_LANGUAGE;
  }
}
