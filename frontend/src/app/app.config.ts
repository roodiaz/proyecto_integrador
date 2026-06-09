import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { TranslateLoader, provideTranslateService } from '@ngx-translate/core';
import { TranslateHttpLoader, TRANSLATE_HTTP_LOADER_CONFIG } from '@ngx-translate/http-loader';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    { provide: TRANSLATE_HTTP_LOADER_CONFIG, useValue: { prefix: '/assets/i18n/', suffix: '.json' } },
    provideTranslateService({
      lang: 'es',
      fallbackLang: 'es',
      loader: {
        provide: TranslateLoader,
        useClass: TranslateHttpLoader
      }
    })
  ]
};
