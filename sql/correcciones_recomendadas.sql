/*
  Correcciones recomendadas para script.sql (base de datos Refaccionaria)
  ------------------------------------------------------------------------
  Los siguientes procedimientos almacenados comparan el parámetro consigo
  mismo (por ejemplo "WHERE @id_parte = @id_parte") en vez de compararlo
  contra la columna de la tabla. Esa condición siempre es verdadera, así
  que cada uno de ellos BORRA TODAS LAS FILAS de su tabla, sin importar
  qué id se les pase.

  La aplicación de Windows Forms que acompaña este script NO llama a estos
  procedimientos tal cual: para las operaciones de borrado afectadas usa
  una instrucción SQL directa con el filtro correcto. Aun así, se
  recomienda corregir los procedimientos en la base de datos para que
  cualquier otro cliente (u otra herramienta) que los use no borre la
  tabla completa por accidente.

  Ejecuta este script contra la base de datos Refaccionaria para reemplazar
  las versiones con el error por las versiones corregidas.
*/
USE [Refaccionaria]
GO

ALTER PROCEDURE [dbo].[spPartesEliminar]
    @id_parte INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Partes WHERE id_parte = @id_parte;
    SELECT @@ROWCOUNT AS RegistrosEliminados;
END;
GO

ALTER PROCEDURE [dbo].[spRelAnioModeloEliminar]
    @id_relAnioModelo INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RelAnioModelo WHERE id_relAnioModelo = @id_relAnioModelo;
    SELECT @@ROWCOUNT AS RegistrosEliminados;
END;
GO

ALTER PROCEDURE [dbo].[spRelEquivalenciasEliminar]
    @id_relEquivalencia INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RelEquivalencias WHERE id_relEquivalencias = @id_relEquivalencia;
    SELECT @@ROWCOUNT AS RegistrosEliminados;
END;
GO

ALTER PROCEDURE [dbo].[spRelMarcaAnioEliminar]
    @id_relMarcaAnio INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RelMarcaAnio WHERE id_relMarcaAnio = @id_relMarcaAnio;
    SELECT @@ROWCOUNT AS RegistrosEliminados;
END;
GO

ALTER PROCEDURE [dbo].[spRelPartesVersionEliminar]
    @id_relParteVersion INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RelPartesVersion WHERE id_relParteVersion = @id_relParteVersion;
    SELECT @@ROWCOUNT AS RegistrosEliminados;
END;
GO

-- Nota: este procedimiento borra de la tabla Version (no de una tabla "Rel...",
-- pese a su nombre). Se deja funcionalmente igual, solo corrigiendo el filtro.
ALTER PROCEDURE [dbo].[spRelVersionEliminar]
    @id_version INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Version WHERE version_id = @id_version;
    SELECT @@ROWCOUNT AS RegistrosEliminados;
END;
GO

ALTER PROCEDURE [dbo].[spUnidadesCatEliminar]
    @id_unidadesCat INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM UnidadesCat WHERE id_unidadesCat = @id_unidadesCat;
    SELECT @@ROWCOUNT AS RegistrosEliminados;
END;
GO
