import sqlite3
conn = sqlite3.connect('app.db')
conn.execute("INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260518020246_AddInspecciones', '9.0.15')")
conn.commit()
conn.close()
print('OK')