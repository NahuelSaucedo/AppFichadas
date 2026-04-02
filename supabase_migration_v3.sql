-- ============================================================
-- MIGRATION v3: Empleado con ubicación y horario asignados
-- Ejecutar en Supabase → SQL Editor
-- ============================================================

-- 1. Crear tabla horarios (si no existe)
CREATE TABLE IF NOT EXISTS horarios (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  nombre text NOT NULL,
  hora_entrada time NOT NULL,
  hora_salida  time NOT NULL,
  tolerancia_minutos int NOT NULL DEFAULT 15,
  activo boolean DEFAULT true,
  created_at timestamptz DEFAULT now()
);

-- 2. Agregar FK ubicacion_id y horario_id a empleados
ALTER TABLE empleados
  ADD COLUMN IF NOT EXISTS ubicacion_id uuid REFERENCES ubicaciones(id) ON DELETE SET NULL,
  ADD COLUMN IF NOT EXISTS horario_id   uuid REFERENCES horarios(id)    ON DELETE SET NULL;

-- 3. Datos de ejemplo: horarios
INSERT INTO horarios (nombre, hora_entrada, hora_salida, tolerancia_minutos) VALUES
  ('Turno mañana',  '08:00', '17:00', 15),
  ('Turno tarde',   '14:00', '22:00', 15),
  ('Turno noche',   '22:00', '06:00', 15),
  ('Jornada completa', '07:00', '18:00', 30)
ON CONFLICT DO NOTHING;

-- ============================================================
-- VERIFICAR
-- ============================================================
-- SELECT e.nombre, e.apellido, u.nombre as ubicacion, h.nombre as horario
-- FROM empleados e
-- LEFT JOIN ubicaciones u ON u.id = e.ubicacion_id
-- LEFT JOIN horarios h ON h.id = e.horario_id;
