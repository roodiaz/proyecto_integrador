@echo off
setlocal enabledelayedexpansion

:: ======================================
:: Leer VERSION desde .env
:: ======================================
set version=
for /f "usebackq tokens=1,2 delims==" %%A in (".env") do (
    if "%%A"=="VERSION" set version=%%B
)

IF "%version%"=="" (
    echo No se encontro la variable VERSION en .env. Saliendo...
    pause
    exit /b 1
)

set destPath=%~dp0..\release\v%version%

:: Opciones de log para docker build:
:: --quiet           -> oculta casi todo el log del build y solo muestra errores
:: --progress=plain  -> muestra logs simples sin el formato moderno (#1 #2 DONE)
:: --progress=auto   -> modo por defecto de Docker (log detallado con progreso)
set DOCKER_BUILD_OPTS=--quiet

echo ======================================
echo  Generando release v%version%
echo  Destino: %destPath%
echo ======================================

:: Limpiar o crear carpeta de publicacion
IF NOT EXIST "%destPath%" GOTO :CREATESUBDIRS
echo Depurando directorio %destPath%
rd /S /Q "%destPath%"
echo Depuracion finalizada.

:CREATESUBDIRS
MD "%destPath%"

:: ======================================
:: investlab.api
:: ======================================
echo Comenzando la construccion de la imagen Docker para API...
docker compose -f docker-compose.yml build %DOCKER_BUILD_OPTS% investlab.api
IF %ERRORLEVEL% NEQ 0 (
    echo Error en la construccion de la imagen Docker para API. Saliendo...
    pause
    exit /b %ERRORLEVEL%
)
echo Imagen API construida exitosamente.

echo Guardando la imagen API en formato .tar...
docker save -o "%destPath%\investlabapi_v%version%.tar" investlabapi:latest
IF %ERRORLEVEL% NEQ 0 (
    echo Error al guardar la imagen API. Saliendo...
    pause
    exit /b %ERRORLEVEL%
)
echo Imagen API guardada correctamente.

:: ======================================
:: investlab.workers
:: ======================================
echo Comenzando la construccion de la imagen Docker para Workers...
docker compose -f docker-compose.yml build %DOCKER_BUILD_OPTS% investlab.workers
IF %ERRORLEVEL% NEQ 0 (
    echo Error en la construccion de la imagen Docker para Workers. Saliendo...
    pause
    exit /b %ERRORLEVEL%
)
echo Imagen Workers construida exitosamente.

echo Guardando la imagen Workers en formato .tar...
docker save -o "%destPath%\investlabworkers_v%version%.tar" investlabworkers:latest
IF %ERRORLEVEL% NEQ 0 (
    echo Error al guardar la imagen Workers. Saliendo...
    pause
    exit /b %ERRORLEVEL%
)
echo Imagen Workers guardada correctamente.

:: ======================================
:: investlab.frontend
:: ======================================
echo Comenzando la construccion de la imagen Docker para Frontend...
docker compose -f docker-compose.yml build %DOCKER_BUILD_OPTS% investlab.frontend
IF %ERRORLEVEL% NEQ 0 (
    echo Error en la construccion de la imagen Docker para Frontend. Saliendo...
    pause
    exit /b %ERRORLEVEL%
)
echo Imagen Frontend construida exitosamente.

echo Guardando la imagen Frontend en formato .tar...
docker save -o "%destPath%\investlabfrontend_v%version%.tar" investlabfrontend:latest
IF %ERRORLEVEL% NEQ 0 (
    echo Error al guardar la imagen Frontend. Saliendo...
    pause
    exit /b %ERRORLEVEL%
)
echo Imagen Frontend guardada correctamente.

:: ======================================
:: Copiar archivos de configuracion
:: ======================================
echo Copiando archivos .env.example y docker-compose.yml a %destPath%...
copy .env.example "%destPath%\" >nul
copy docker-compose.yml "%destPath%\" >nul
IF EXIST docker-compose.override.yml copy docker-compose.override.yml "%destPath%\" >nul
IF %ERRORLEVEL% NEQ 0 (
    echo Error al copiar los archivos. Saliendo...
    pause
    exit /b %ERRORLEVEL%
)
echo Archivos copiados correctamente.

echo.
echo ======================================
echo  Release v%version% generado con exito.
echo  Ruta del paquete: %destPath%
echo ======================================
echo.

pause

set destPath=
set version=

endlocal
