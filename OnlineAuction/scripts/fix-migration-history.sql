USE online_auction;

INSERT IGNORE INTO __ef_migrations_history (MigrationId, ProductVersion)
VALUES ('20260627143448_AddComplaintsTable', '9.0.17');

SELECT MigrationId, ProductVersion
FROM __ef_migrations_history
ORDER BY MigrationId;
