USE Refaccionaria
GO
/* ============================================================================
   Seed de catálogo para RefaccionariaApp (SQL Server / T-SQL)
   ----------------------------------------------------------------------------
   Llena: Anio (1965-2026), TipoRefaCat (categorías de partes),
          Marca, Modelo, Version, RelMarcaAnio, RelAnioModelo.

   Alcance: marcas conocidas comercializadas en México y EUA.
   Versiones: curadas por marca (trims reales del fabricante).
   Relaciones: cada modelo se liga a los años >= su año de inicio.

   Es IDEMPOTENTE: se puede correr varias veces sin duplicar
   (usa NOT EXISTS por nombre). No toca datos existentes.

   Ejecutar todo el archivo en una sola conexión (usa tablas temporales #).
   ============================================================================ */

SET NOCOUNT ON;

/* ----------------------------------------------------------------------------
   1) AÑOS: 1965 .. 2026
   ---------------------------------------------------------------------------- */
DECLARE @anio INT = 1965;
DECLARE @anioFin INT = 2026;

WHILE @anio <= @anioFin
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Anio WHERE nombre = CAST(@anio AS NVARCHAR(10)))
        INSERT INTO dbo.Anio (nombre) VALUES (CAST(@anio AS NVARCHAR(10)));
    SET @anio += 1;
END

/* ----------------------------------------------------------------------------
   2) CATEGORÍAS DE PARTES (TipoRefaCat)
   izq / der = bandera (1 = la parte tiene variante izquierda/derecha, 0 = no)
   ---------------------------------------------------------------------------- */
DECLARE @Cat TABLE (nombre NVARCHAR(255), izq INT, der INT);
INSERT INTO @Cat (nombre, izq, der) VALUES
 -- Frenos
 (N'Balatas',                    0,0),
 (N'Discos de freno',            1,1),
 (N'Tambores de freno',          1,1),
 (N'Caliper',                    1,1),
 (N'Cilindro maestro de freno',  0,0),
 (N'Bomba de freno',             0,0),
 (N'Cables de freno',            0,0),
 -- Suspensión
 (N'Amortiguadores',             1,1),
 (N'Resortes / espirales',       1,1),
 (N'Rótulas',                    1,1),
 (N'Terminales de suspensión',   1,1),
 (N'Horquillas',                 1,1),
 (N'Bujes de suspensión',        1,1),
 (N'Bieletas',                   1,1),
 (N'Bases de amortiguador',      1,1),
 (N'Baleros de rueda',           1,1),
 -- Motor
 (N'Bujías',                     0,0),
 (N'Bandas',                     0,0),
 (N'Banda de distribución',      0,0),
 (N'Filtro de aceite',           0,0),
 (N'Filtro de aire',             0,0),
 (N'Bomba de agua',              0,0),
 (N'Bomba de aceite',            0,0),
 (N'Empaques',                   0,0),
 (N'Junta de cabeza',            0,0),
 (N'Soportes de motor',          1,1),
 (N'Inyectores',                 0,0),
 (N'Bobina de encendido',        0,0),
 -- Sistema eléctrico
 (N'Alternador',                 0,0),
 (N'Marcha / motor de arranque', 0,0),
 (N'Batería',                    0,0),
 (N'Faros',                      1,1),
 (N'Calaveras',                  1,1),
 (N'Focos',                      0,0),
 (N'Sensores',                   0,0),
 (N'Fusibles y relevadores',     0,0),
 -- Transmisión / embrague
 (N'Clutch / embrague',          0,0),
 (N'Volante motor',              0,0),
 (N'Flechas homocinéticas',      1,1),
 (N'Juntas homocinéticas',       1,1),
 (N'Cruceta cardán',             0,0),
 -- Enfriamiento
 (N'Radiador',                   0,0),
 (N'Termostato',                 0,0),
 (N'Ventilador / electroventilador', 0,0),
 (N'Mangueras de radiador',      0,0),
 (N'Depósito de anticongelante', 0,0),
 -- Dirección
 (N'Cremallera de dirección',    0,0),
 (N'Bomba de dirección',         0,0),
 (N'Terminales de dirección',    1,1),
 -- Carrocería
 (N'Cofre',                      0,0),
 (N'Salpicadera',                1,1),
 (N'Defensa',                    0,0),
 (N'Espejos',                    1,1),
 (N'Parrilla',                   0,0),
 (N'Molduras',                   1,1),
 (N'Manijas',                    1,1),
 -- Filtros / combustible
 (N'Filtro de gasolina',         0,0),
 (N'Filtro de cabina',           0,0),
 (N'Bomba de gasolina',          0,0),
 -- Escape
 (N'Catalizador',                0,0),
 (N'Silenciador / mofle',        0,0),
 (N'Múltiple de escape',         0,0),
 -- Limpieza / varios
 (N'Plumas limpiaparabrisas',    1,1),
 (N'Aceites y lubricantes',      0,0),
 (N'Anticongelante',             0,0);

INSERT INTO dbo.TipoRefaCat (nombre, izq, der)
SELECT c.nombre, c.izq, c.der
FROM @Cat c
WHERE NOT EXISTS (SELECT 1 FROM dbo.TipoRefaCat t WHERE t.nombre = c.nombre);

/* ----------------------------------------------------------------------------
   3) STAGING: Marca / Modelo / AnioInicio (año en que arrancó el modelo)
   ---------------------------------------------------------------------------- */
CREATE TABLE #Modelos (Marca NVARCHAR(100), Modelo NVARCHAR(100), AnioInicio INT);
INSERT INTO #Modelos (Marca, Modelo, AnioInicio) VALUES
 -- NISSAN
 (N'Nissan', N'Sentra',      1982),
 (N'Nissan', N'Versa',       2007),
 (N'Nissan', N'Altima',      1993),
 (N'Nissan', N'Maxima',      1981),
 (N'Nissan', N'March',       2011),
 (N'Nissan', N'Note',        2014),
 (N'Nissan', N'Kicks',       2016),
 (N'Nissan', N'X-Trail',     2001),
 (N'Nissan', N'Rogue',       2008),
 (N'Nissan', N'Frontier',    1998),
 (N'Nissan', N'NP300',       2008),
 (N'Nissan', N'Pathfinder',  1986),
 (N'Nissan', N'Murano',      2003),
 (N'Nissan', N'Tsuru',       1984),
 (N'Nissan', N'Titan',       2004),
 (N'Nissan', N'Leaf',        2011),
 -- TOYOTA
 (N'Toyota', N'Corolla',       1966),
 (N'Toyota', N'Camry',         1983),
 (N'Toyota', N'Yaris',         2006),
 (N'Toyota', N'Avanza',        2004),
 (N'Toyota', N'Hilux',         1968),
 (N'Toyota', N'Tacoma',        1995),
 (N'Toyota', N'Tundra',        2000),
 (N'Toyota', N'RAV4',          1996),
 (N'Toyota', N'Highlander',    2001),
 (N'Toyota', N'4Runner',       1984),
 (N'Toyota', N'Prius',         2001),
 (N'Toyota', N'Sienna',        1998),
 (N'Toyota', N'Land Cruiser',  1965),
 (N'Toyota', N'Sequoia',       2001),
 (N'Toyota', N'Corolla Cross', 2021),
 -- HONDA
 (N'Honda', N'Civic',     1973),
 (N'Honda', N'Accord',    1976),
 (N'Honda', N'City',      2006),
 (N'Honda', N'Fit',       2007),
 (N'Honda', N'CR-V',      1997),
 (N'Honda', N'HR-V',      2016),
 (N'Honda', N'Pilot',     2003),
 (N'Honda', N'Odyssey',   1995),
 (N'Honda', N'Ridgeline', 2006),
 (N'Honda', N'Passport',  2019),
 -- VOLKSWAGEN
 (N'Volkswagen', N'Jetta',            1980),
 (N'Volkswagen', N'Golf',             1975),
 (N'Volkswagen', N'Passat',           1974),
 (N'Volkswagen', N'Tiguan',           2008),
 (N'Volkswagen', N'Beetle',           1998),
 (N'Volkswagen', N'Sedán (Vocho)',    1965),
 (N'Volkswagen', N'Vento',            2014),
 (N'Volkswagen', N'Polo',             2003),
 (N'Volkswagen', N'Bora',             1999),
 (N'Volkswagen', N'Teramont/Atlas',   2018),
 (N'Volkswagen', N'Taos',             2021),
 (N'Volkswagen', N'Amarok',           2010),
 -- CHEVROLET
 (N'Chevrolet', N'Aveo',       2004),
 (N'Chevrolet', N'Beat',       2018),
 (N'Chevrolet', N'Spark',      2011),
 (N'Chevrolet', N'Cavalier',   1982),
 (N'Chevrolet', N'Malibu',     1997),
 (N'Chevrolet', N'Cruze',      2009),
 (N'Chevrolet', N'Silverado',  1999),
 (N'Chevrolet', N'Cheyenne',   1999),
 (N'Chevrolet', N'Tahoe',      1995),
 (N'Chevrolet', N'Suburban',   1965),
 (N'Chevrolet', N'Trax',       2013),
 (N'Chevrolet', N'Equinox',    2005),
 (N'Chevrolet', N'Colorado',   2004),
 (N'Chevrolet', N'Camaro',     1967),
 (N'Chevrolet', N'Corvette',   1965),
 (N'Chevrolet', N'Blazer',     1969),
 -- FORD
 (N'Ford', N'Focus',      2000),
 (N'Ford', N'Fiesta',     1976),
 (N'Ford', N'Mustang',    1965),
 (N'Ford', N'Fusion',     2006),
 (N'Ford', N'Escape',     2001),
 (N'Ford', N'EcoSport',   2013),
 (N'Ford', N'Explorer',   1991),
 (N'Ford', N'Edge',       2007),
 (N'Ford', N'Ranger',     1983),
 (N'Ford', N'F-150',      1975),
 (N'Ford', N'Lobo',       1997),
 (N'Ford', N'Bronco',     1966),
 (N'Ford', N'Expedition', 1997),
 (N'Ford', N'Maverick',   2022),
 -- DODGE
 (N'Dodge', N'Attitude',   2006),
 (N'Dodge', N'Neon',       1995),
 (N'Dodge', N'Charger',    1966),
 (N'Dodge', N'Challenger', 1970),
 (N'Dodge', N'Journey',    2009),
 (N'Dodge', N'Durango',    1998),
 (N'Dodge', N'Dart',       2013),
 -- RAM
 (N'RAM', N'1500',      2011),
 (N'RAM', N'2500',      2011),
 (N'RAM', N'700',       2015),
 (N'RAM', N'1000',      2017),
 (N'RAM', N'ProMaster', 2014),
 -- JEEP
 (N'Jeep', N'Grand Cherokee', 1993),
 (N'Jeep', N'Cherokee',       1974),
 (N'Jeep', N'Wrangler',       1987),
 (N'Jeep', N'Compass',        2007),
 (N'Jeep', N'Renegade',       2015),
 (N'Jeep', N'Patriot',        2007),
 (N'Jeep', N'Gladiator',      2020),
 -- CHRYSLER
 (N'Chrysler', N'300',       2005),
 (N'Chrysler', N'Pacifica',  2004),
 (N'Chrysler', N'PT Cruiser',2000),
 (N'Chrysler', N'Voyager',   1988),
 (N'Chrysler', N'Town & Country', 1990),
 -- GMC
 (N'GMC', N'Sierra',  1999),
 (N'GMC', N'Yukon',   1992),
 (N'GMC', N'Terrain', 2010),
 (N'GMC', N'Acadia',  2007),
 (N'GMC', N'Canyon',  2004),
 -- HYUNDAI
 (N'Hyundai', N'Accent',     2000),
 (N'Hyundai', N'Elantra',    1990),
 (N'Hyundai', N'Sonata',     1989),
 (N'Hyundai', N'Tucson',     2005),
 (N'Hyundai', N'Santa Fe',   2001),
 (N'Hyundai', N'Creta',      2016),
 (N'Hyundai', N'Grand i10',  2014),
 (N'Hyundai', N'Ioniq',      2017),
 (N'Hyundai', N'Palisade',   2020),
 (N'Hyundai', N'Kona',       2018),
 -- KIA
 (N'Kia', N'Rio',      2001),
 (N'Kia', N'Forte',    2009),
 (N'Kia', N'Sportage', 1995),
 (N'Kia', N'Sorento',  2003),
 (N'Kia', N'Soul',     2009),
 (N'Kia', N'Seltos',   2020),
 (N'Kia', N'K5',       2021),
 (N'Kia', N'Sedona',   2006),
 (N'Kia', N'Telluride',2020),
 (N'Kia', N'Picanto',  2004),
 -- MAZDA
 (N'Mazda', N'Mazda2',      2007),
 (N'Mazda', N'Mazda3',      2004),
 (N'Mazda', N'Mazda6',      2003),
 (N'Mazda', N'CX-3',        2015),
 (N'Mazda', N'CX-30',       2020),
 (N'Mazda', N'CX-5',        2012),
 (N'Mazda', N'CX-9',        2007),
 (N'Mazda', N'MX-5 Miata',  1989),
 (N'Mazda', N'BT-50',       2011),
 -- MITSUBISHI
 (N'Mitsubishi', N'Lancer',        1973),
 (N'Mitsubishi', N'Mirage',        2012),
 (N'Mitsubishi', N'Outlander',     2001),
 (N'Mitsubishi', N'Montero',       1982),
 (N'Mitsubishi', N'L200',          1996),
 (N'Mitsubishi', N'Eclipse Cross', 2018),
 (N'Mitsubishi', N'ASX',           2010),
 -- SUBARU
 (N'Subaru', N'Impreza',   1993),
 (N'Subaru', N'Legacy',    1989),
 (N'Subaru', N'Outback',   1995),
 (N'Subaru', N'Forester',  1997),
 (N'Subaru', N'Crosstrek', 2013),
 (N'Subaru', N'WRX',       2015),
 (N'Subaru', N'Ascent',    2018),
 -- BUICK
 (N'Buick', N'Regal',    1973),
 (N'Buick', N'LaCrosse', 2005),
 (N'Buick', N'Enclave',  2008),
 (N'Buick', N'Encore',   2013),
 (N'Buick', N'Envision', 2016);

/* ----------------------------------------------------------------------------
   4) STAGING: Versiones (trims) por marca
   ---------------------------------------------------------------------------- */
CREATE TABLE #Trims (Marca NVARCHAR(100), Version NVARCHAR(100));
INSERT INTO #Trims (Marca, Version) VALUES
 (N'Nissan', N'Base'), (N'Nissan', N'Sense'), (N'Nissan', N'Advance'), (N'Nissan', N'Exclusive'), (N'Nissan', N'SR'), (N'Nissan', N'Platinum'),
 (N'Toyota', N'Base'), (N'Toyota', N'LE'), (N'Toyota', N'XLE'), (N'Toyota', N'SE'), (N'Toyota', N'Limited'), (N'Toyota', N'SR5'), (N'Toyota', N'TRD'),
 (N'Honda', N'LX'), (N'Honda', N'EX'), (N'Honda', N'EX-L'), (N'Honda', N'Sport'), (N'Honda', N'Touring'),
 (N'Volkswagen', N'Trendline'), (N'Volkswagen', N'Comfortline'), (N'Volkswagen', N'Highline'), (N'Volkswagen', N'Sportline'), (N'Volkswagen', N'GLI'), (N'Volkswagen', N'GTI'),
 (N'Chevrolet', N'LS'), (N'Chevrolet', N'LT'), (N'Chevrolet', N'LTZ'), (N'Chevrolet', N'Premier'), (N'Chevrolet', N'RS'), (N'Chevrolet', N'Z71'),
 (N'Ford', N'S'), (N'Ford', N'SE'), (N'Ford', N'SEL'), (N'Ford', N'Titanium'), (N'Ford', N'XLT'), (N'Ford', N'Lariat'), (N'Ford', N'ST'),
 (N'Dodge', N'SE'), (N'Dodge', N'SXT'), (N'Dodge', N'GT'), (N'Dodge', N'R/T'), (N'Dodge', N'SRT'),
 (N'RAM', N'Tradesman'), (N'RAM', N'Big Horn'), (N'RAM', N'Laramie'), (N'RAM', N'Rebel'), (N'RAM', N'Limited'),
 (N'Jeep', N'Sport'), (N'Jeep', N'Latitude'), (N'Jeep', N'Limited'), (N'Jeep', N'Trailhawk'), (N'Jeep', N'Overland'),
 (N'Chrysler', N'LX'), (N'Chrysler', N'Touring'), (N'Chrysler', N'Limited'), (N'Chrysler', N'S'),
 (N'GMC', N'SLE'), (N'GMC', N'SLT'), (N'GMC', N'Denali'), (N'GMC', N'AT4'), (N'GMC', N'Elevation'),
 (N'Hyundai', N'GL'), (N'Hyundai', N'GLS'), (N'Hyundai', N'Limited'), (N'Hyundai', N'Sport'), (N'Hyundai', N'Premium'),
 (N'Kia', N'LX'), (N'Kia', N'EX'), (N'Kia', N'SX'), (N'Kia', N'GT-Line'),
 (N'Mazda', N'i'), (N'Mazda', N's'), (N'Mazda', N'Touring'), (N'Mazda', N'Grand Touring'), (N'Mazda', N'Signature'),
 (N'Mitsubishi', N'GLX'), (N'Mitsubishi', N'GLS'), (N'Mitsubishi', N'SE'), (N'Mitsubishi', N'SEL'),
 (N'Subaru', N'Base'), (N'Subaru', N'Premium'), (N'Subaru', N'Limited'), (N'Subaru', N'Sport'), (N'Subaru', N'Touring'),
 (N'Buick', N'Preferred'), (N'Buick', N'Essence'), (N'Buick', N'Premium'), (N'Buick', N'Avenir');

/* ----------------------------------------------------------------------------
   5) INSERT MARCAS
   ---------------------------------------------------------------------------- */
INSERT INTO dbo.Marca (nombre)
SELECT DISTINCT m.Marca
FROM #Modelos m
WHERE NOT EXISTS (SELECT 1 FROM dbo.Marca k WHERE k.nombre = m.Marca);

/* ----------------------------------------------------------------------------
   6) INSERT MODELOS (ligados a su marca)
   ---------------------------------------------------------------------------- */
INSERT INTO dbo.Modelo (nombre, id_marca)
SELECT m.Modelo, k.id_marca
FROM #Modelos m
JOIN dbo.Marca k ON k.nombre = m.Marca
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Modelo mo
    WHERE mo.nombre = m.Modelo AND mo.id_marca = k.id_marca
);

/* ----------------------------------------------------------------------------
   7) INSERT VERSIONES (trims de la marca aplicados a cada modelo de esa marca)
   ---------------------------------------------------------------------------- */
INSERT INTO dbo.[Version] (modelo_id, nombre)
SELECT mo.id_modelo, t.Version
FROM #Modelos m
JOIN dbo.Marca  k  ON k.nombre = m.Marca
JOIN dbo.Modelo mo ON mo.nombre = m.Modelo AND mo.id_marca = k.id_marca
JOIN #Trims     t  ON t.Marca = m.Marca
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.[Version] v
    WHERE v.modelo_id = mo.id_modelo AND v.nombre = t.Version
);

/* ----------------------------------------------------------------------------
   8) RelMarcaAnio: cada marca ligada a los años >= año más antiguo de sus modelos
   ---------------------------------------------------------------------------- */
;WITH MarcaDesde AS (
    SELECT k.id_marca, MIN(m.AnioInicio) AS anioDesde
    FROM #Modelos m
    JOIN dbo.Marca k ON k.nombre = m.Marca
    GROUP BY k.id_marca
)
INSERT INTO dbo.RelMarcaAnio (marca_id, anio_id)
SELECT md.id_marca, a.id_anio
FROM MarcaDesde md
JOIN dbo.Anio a ON TRY_CAST(a.nombre AS INT) >= md.anioDesde
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RelMarcaAnio r
    WHERE r.marca_id = md.id_marca AND r.anio_id = a.id_anio
);

/* ----------------------------------------------------------------------------
   9) RelAnioModelo: cada modelo ligado a los años >= su año de inicio
   ---------------------------------------------------------------------------- */
INSERT INTO dbo.RelAnioModelo (modelo_id, anio_id)
SELECT mo.id_modelo, a.id_anio
FROM #Modelos m
JOIN dbo.Marca  k  ON k.nombre = m.Marca
JOIN dbo.Modelo mo ON mo.nombre = m.Modelo AND mo.id_marca = k.id_marca
JOIN dbo.Anio   a  ON TRY_CAST(a.nombre AS INT) >= m.AnioInicio
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RelAnioModelo r
    WHERE r.modelo_id = mo.id_modelo AND r.anio_id = a.id_anio
);

/* ----------------------------------------------------------------------------
   Limpieza
   ---------------------------------------------------------------------------- */
DROP TABLE #Modelos;
DROP TABLE #Trims;

/* ----------------------------------------------------------------------------
   Resumen
   ---------------------------------------------------------------------------- */
SELECT 'Anio'          AS Tabla, COUNT(*) AS Registros FROM dbo.Anio
UNION ALL SELECT 'TipoRefaCat',   COUNT(*) FROM dbo.TipoRefaCat
UNION ALL SELECT 'Marca',         COUNT(*) FROM dbo.Marca
UNION ALL SELECT 'Modelo',        COUNT(*) FROM dbo.Modelo
UNION ALL SELECT 'Version',       COUNT(*) FROM dbo.[Version]
UNION ALL SELECT 'RelMarcaAnio',  COUNT(*) FROM dbo.RelMarcaAnio
UNION ALL SELECT 'RelAnioModelo', COUNT(*) FROM dbo.RelAnioModelo;
