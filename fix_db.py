import sqlite3
conn = sqlite3.connect('app.db')
c = conn.cursor()
c.execute("ALTER TABLE Clientes ADD COLUMN FechaRegistro TEXT NOT NULL DEFAULT '2026-01-01'")
conn.commit()
conn.close()
print('FechaRegistro OK')