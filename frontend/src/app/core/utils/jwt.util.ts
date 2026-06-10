/**
 * Decodifica el payload de un JWT (sin verificar la firma) para leer sus claims.
 * @param token Token JWT en formato `header.payload.signature`.
 * @returns El payload decodificado, o `null` si el token no tiene un formato válido.
 */
export function decodeJwtPayload(token: string): Record<string, unknown> | null {
    try {
        const payload = token.split('.')[1];

        if (!payload) {
            return null;
        }

        const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
        const json = decodeURIComponent(
            atob(base64)
                .split('')
                .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                .join('')
        );

        return JSON.parse(json);
    } catch {
        return null;
    }
}

/**
 * Obtiene la fecha de expiración (claim `exp`) de un JWT, en milisegundos desde epoch.
 * @param token Token JWT a inspeccionar.
 * @returns El timestamp de expiración en milisegundos, o `null` si no puede determinarse.
 */
export function getJwtExpiration(token: string): number | null {
    const payload = decodeJwtPayload(token);
    const exp = payload?.['exp'];

    return typeof exp === 'number' ? exp * 1000 : null;
}
