-- Create login at server level
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'std')
BEGIN
    CREATE LOGIN std WITH PASSWORD = '1k2m3b4T!';
END
GO

-- Create database if it does not exist
IF DB_ID('StudentDB') IS NULL
BEGIN
    CREATE DATABASE StudentDB;
END
GO

-- Switch to database
USE StudentDB;
GO

-- Create user mapped to login if not exists
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'std')
BEGIN
    CREATE USER std FOR LOGIN std;
END
GO

-- Give db_owner rights
ALTER ROLE db_owner ADD MEMBER std;
GO