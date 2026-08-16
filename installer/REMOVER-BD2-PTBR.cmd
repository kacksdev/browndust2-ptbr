@echo off
setlocal
title Remover - Brown Dust 2 PT-BR
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Remover-BD2PTBR.ps1"
if errorlevel 1 (
  echo.
  echo A remocao nao foi concluida. Leia a mensagem acima.
) else (
  echo.
  echo Remocao concluida.
)
pause
