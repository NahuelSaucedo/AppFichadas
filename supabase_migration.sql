-- ============================================================
-- MIGRATION: Agregar columnas de geolocalización a ubicaciones
-- Ejecutar en Supabase → SQL Editor
-- ============================================================

-- 1. Agregar columnas de coordenadas y radio a la tabla ubicaciones
ALTER TABLE ubicaciones
  ADD COLUMN IF NOT EXISTS latitud  double precision NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS longitud double precision NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS radio_metros integer NOT NULL DEFAULT 100;

-- 2. Datos de ejemplo (reemplazá con tus coordenadas reales)
INSERT INTO ubicaciones (nombre, direccion, latitud, longitud, radio_metros) VALUES
  ('Oficina Central',  'Av. Corrientes 1234, CABA',     -34.603722, -58.381592, 150),
  ('Depósito Norte',   'Av. del Trabajo 1200, CABA',    -34.590000, -58.370000, 200),
  ('Sucursal Sur',     'Av. San Juan 3500, CABA',       -34.620000, -58.390000, 100)
ON CONFLICT DO NOTHING;

-- 3. Datos de empleados de prueba
INSERT INTO empleados (legajo, nombre, apellido, dni, activo) VALUES
  ('0001', 'Juan',   'García',   '30123456', true),
  ('0002', 'María',  'López',    '32456789', true),
  ('0003', 'Carlos', 'Martínez', '28901234', true)
ON CONFLICT (legajo) DO NOTHING;

-- ============================================================
-- VERIFICACIÓN: Ejecutá estas queries para confirmar
-- ============================================================
-- SELECT * FROM ubicaciones;
-- SELECT * FROM empleados;
