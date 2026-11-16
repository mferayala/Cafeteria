@echo off
rem Prepara la PC que guarda la base de datos (la que tiene XAMPP).
rem Doble clic: pide permiso de administrador y ejecuta herramientas\instalar_servidor.ps1.
rem Ver PASO_A_PASO_BASE_DE_DATOS.md.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell -Verb RunAs -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File','\"%~dp0herramientas\instalar_servidor.ps1\"'"
