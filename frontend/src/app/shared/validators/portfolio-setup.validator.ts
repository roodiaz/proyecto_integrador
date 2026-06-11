/** Longitud mínima del nombre de portfolio, debe coincidir con `PortfolioPolicy.NameMinLength` del backend. */
export const PORTFOLIO_NAME_MIN_LENGTH = 3;

/** Longitud máxima del nombre de portfolio, debe coincidir con `PortfolioPolicy.NameMaxLength` del backend. */
export const PORTFOLIO_NAME_MAX_LENGTH = 100;

/** Expresión regular del nombre de portfolio (letras, números y espacios), equivalente a `PortfolioPolicy` del backend. */
export const PORTFOLIO_NAME_PATTERN = /^[\p{L}\p{N}\s]+$/u;

/** Saldo inicial mínimo permitido (USD), debe coincidir con `PortfolioPolicy.MinInitialBalance` del backend. */
export const INITIAL_BALANCE_MIN = 1000;

/** Saldo inicial máximo permitido (USD), debe coincidir con `PortfolioPolicy.MaxInitialBalance` del backend. */
export const INITIAL_BALANCE_MAX = 1_000_000;

/** Saldo inicial por defecto (USD) propuesto al usuario en el wizard de configuración. */
export const INITIAL_BALANCE_DEFAULT = 10000;
