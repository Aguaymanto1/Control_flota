import sqlite3
conn = sqlite3.connect('app.db')
conn.execute("INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260520085901_SyncModelo', '9.0.15')")
conn.commit()
conn.close()
print('OK')