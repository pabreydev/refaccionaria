/*
  Permisos para los usuarios de Windows de la PC (lo ejecuta el instalador).
  ------------------------------------------------------------------------
  La aplicación se conecta con autenticación de Windows. Quien instala SQL
  Server queda como administrador, pero los demás usuarios de la PC no
  tendrían acceso; por eso se da acceso al grupo local "Usuarios"
  (BUILTIN\Users, SID S-1-5-32-545; su nombre cambia según el idioma de
  Windows, por eso se obtiene a partir del SID).

  - db_owner en Refaccionaria: altas, bajas, cambios y respaldos.
  - CREATE ANY DATABASE: SQL Server lo exige para RESTORE VERIFYONLY, con el
    que la app verifica cada respaldo.

  Se puede ejecutar varias veces.
*/
USE [master]
GO
DECLARE @grupo SYSNAME = SUSER_SNAME(0x01020000000000052000000021020000);
DECLARE @sql NVARCHAR(MAX);

IF SUSER_ID(@grupo) IS NULL
BEGIN
    SET @sql = N'CREATE LOGIN ' + QUOTENAME(@grupo) + N' FROM WINDOWS WITH DEFAULT_DATABASE = [Refaccionaria];';
    EXEC (@sql);
END;

SET @sql = N'GRANT CREATE ANY DATABASE TO ' + QUOTENAME(@grupo) + N';';
EXEC (@sql);
GO
USE [Refaccionaria]
GO
DECLARE @grupo SYSNAME = SUSER_SNAME(0x01020000000000052000000021020000);
DECLARE @sql NVARCHAR(MAX);

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE sid = SUSER_SID(@grupo))
BEGIN
    SET @sql = N'CREATE USER ' + QUOTENAME(@grupo) + N' FOR LOGIN ' + QUOTENAME(@grupo) + N';';
    EXEC (@sql);
END;

SET @sql = N'ALTER ROLE [db_owner] ADD MEMBER ' + QUOTENAME(@grupo) + N';';
EXEC (@sql);
GO
