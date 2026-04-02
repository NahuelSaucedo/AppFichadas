-- ============================================================
-- MIGRATION v2: Tabla horarios
-- Ejecutar en Supabase → SQL Editor
-- ============================================================

-- Tabla de horarios por ubicación
create table horarios (
  id uuid primary key default gen_random_uuid(),
  ubicacion_id uuid references ubicaciones(id) on delete cascade,
  nombre text not null,                    -- Ej: "Turno mañana"
  hora_entrada time not null,              -- Ej: 08:00
  hora_salida  time not null,              -- Ej: 17:00
  tolerancia_minutos int not null default 15,  -- margen antes/después
  activo boolean default true,
  created_at timestamptz default now()
);

-- Datos de ejemplo
INSERT INTO horarios (ubicacion_id, nombre, hora_entrada, hora_salida, tolerancia_minutos)
SELECT id, 'Turno mañana', '08:00', '17:00', 15 FROM ubicaciones LIMIT 1;

-- Verificar
SELECT h.nombre, h.hora_entrada, h.hora_salida, u.nombre as ubicacion
FROM horarios h JOIN ubicaciones u ON u.id = h.ubicacion_id;
