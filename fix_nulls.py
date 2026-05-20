import sqlite3
conn = sqlite3.connect('app.db')
conn.execute("UPDATE Clientes SET Correo = 'sin-correo@pendiente.com' WHERE Correo IS NULL")
conn.execute("UPDATE Clientes SET Estado = 'Activo' WHERE Estado IS NULL OR Estado = ''")
conn.execute("UPDATE Clientes SET FechaRegistro = '2026-01-01' WHERE FechaRegistro IS NULL OR FechaRegistro = ''")
conn.commit()
conn.close()
print('Nulls corregidos')