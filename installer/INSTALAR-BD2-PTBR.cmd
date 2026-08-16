@echo off
setlocal
title Instalador - Brown Dust 2 PT-BR
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Instalar-BD2PTBR.ps1"
if errorlevel 1 (
  echo.
  echo A instalacao nao foi concluida. Leia a mensagem acima.
) else (
  echo.
  echo Instalacao concluida.
)
pause
