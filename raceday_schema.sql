-- RaceDay Database Schema
-- Creates the tables from the ERD and adds some sample data

USE master;
GO

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

CREATE TABLE dbo.Roles (
    RoleId      INT IDENTITY(1,1) NOT NULL,
    RoleName    NVARCHAR(50)      NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);
GO

CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1) NOT NULL,
    RoleId          INT               NOT NULL,
    FullName        NVARCHAR(100)     NOT NULL,
    Email           NVARCHAR(150)     NOT NULL,
    PasswordHash    NVARCHAR(255)     NOT NULL,
    CreatedAt       DATETIME2         NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (RoleId)
);
GO

CREATE TABLE dbo.Events (
    EventId         INT IDENTITY(1,1) NOT NULL,
    OrganiserId     INT               NOT NULL,
    EventName       NVARCHAR(150)     NOT NULL,
    Description     NVARCHAR(1000)    NULL,
    EventDate       DATE              NOT NULL,
    Location        NVARCHAR(200)     NOT NULL,
    DistanceKm      DECIMAL(5,2)      NOT NULL,
    EventType       NVARCHAR(20)      NOT NULL DEFAULT 'Run',
    CreatedAt       DATETIME2         NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CONSTRAINT FK_Events_Users_Organiser FOREIGN KEY (OrganiserId) REFERENCES dbo.Users (UserId)
);
GO

CREATE TABLE dbo.Categories (
    CategoryId       INT IDENTITY(1,1) NOT NULL,
    EventId          INT               NOT NULL,
    CategoryName     NVARCHAR(100)     NOT NULL,
    DistanceKm       DECIMAL(5,2)      NOT NULL,
    EntryFee         DECIMAL(8,2)      NOT NULL DEFAULT 0,
    MaxParticipants  INT               NOT NULL DEFAULT 100,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES dbo.Events (EventId),
    CONSTRAINT UQ_Categories_Event_Name UNIQUE (EventId, CategoryName),
    CONSTRAINT CK_Categories_MaxParticipants CHECK (MaxParticipants > 0)
);
GO

CREATE TABLE dbo.Enrolments (
    EnrolmentId      INT IDENTITY(1,1) NOT NULL,
    CategoryId       INT               NOT NULL,
    ParticipantId    INT               NOT NULL,
    EnrolmentDate    DATETIME2         NOT NULL DEFAULT SYSDATETIME(),
    Status           NVARCHAR(20)      NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT FK_Enrolments_Users_Participant FOREIGN KEY (ParticipantId) REFERENCES dbo.Users (UserId),
    CONSTRAINT UQ_Enrolments_Category_Participant UNIQUE (CategoryId, ParticipantId)
);
GO

CREATE TABLE dbo.Results (
    ResultId        INT IDENTITY(1,1) NOT NULL,
    EnrolmentId     INT               NOT NULL,
    FinishTime      TIME              NULL,
    Position        INT               NULL,
    Status          NVARCHAR(20)      NOT NULL DEFAULT 'Finished',
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolments (EnrolmentId),
    CONSTRAINT UQ_Results_EnrolmentId UNIQUE (EnrolmentId)
);
GO

-- Roles
INSERT INTO dbo.Roles (RoleName) VALUES
    ('Admin'),
    ('Organiser'),
    ('Participant');
GO

-- Users (2 organisers, 2 participants)
INSERT INTO dbo.Users (RoleId, FullName, Email, PasswordHash) VALUES
    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Organiser'),   'Sipho Nkosi',    'sipho.nkosi@raceday.co.za',    'HASH_PLACEHOLDER_1'),
    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Organiser'),   'Lerato Mokoena', 'lerato.mokoena@raceday.co.za', 'HASH_PLACEHOLDER_2'),
    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'), 'Thabo Dlamini',  'thabo.dlamini@example.com',    'HASH_PLACEHOLDER_3'),
    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'), 'Aisha Patel',    'aisha.patel@example.com',      'HASH_PLACEHOLDER_4');
GO

-- Events (3 events, split across the 2 organisers)
INSERT INTO dbo.Events (OrganiserId, EventName, Description, EventDate, Location, DistanceKm, EventType) VALUES
    ((SELECT UserId FROM dbo.Users WHERE Email = 'sipho.nkosi@raceday.co.za'),
        'Gqeberha Bay Run', 'Annual coastal road race along the Gqeberha beachfront.', '2026-11-15', 'Gqeberha, Eastern Cape', 10.00, 'Run'),
    ((SELECT UserId FROM dbo.Users WHERE Email = 'sipho.nkosi@raceday.co.za'),
        'Baakens Valley Trail Challenge', 'Off-road trail ride through the Baakens River valley.', '2026-09-20', 'Baakens Valley, Gqeberha', 25.00, 'Cycle'),
    ((SELECT UserId FROM dbo.Users WHERE Email = 'lerato.mokoena@raceday.co.za'),
        'Nelson Mandela Bay Marathon', 'Full and half marathon through the metro.', '2027-03-08', 'Nelson Mandela Bay', 42.20, 'Run');
GO

-- Categories (each event gets two)
INSERT INTO dbo.Categories (EventId, CategoryName, DistanceKm, EntryFee, MaxParticipants) VALUES
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Gqeberha Bay Run'),               '5km Fun Run',   5.00,  150.00, 300),
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Gqeberha Bay Run'),               '10km Race',     10.00, 200.00, 200),
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Baakens Valley Trail Challenge'), '15km Trail',    15.00, 250.00, 150),
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Baakens Valley Trail Challenge'), '25km Trail',    25.00, 300.00, 100),
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Nelson Mandela Bay Marathon'),    'Half Marathon', 21.10, 350.00, 500),
    ((SELECT EventId FROM dbo.Events WHERE EventName = 'Nelson Mandela Bay Marathon'),    'Full Marathon', 42.20, 450.00, 300);
GO

-- Enrolments
INSERT INTO dbo.Enrolments (CategoryId, ParticipantId, Status) VALUES
    ((SELECT CategoryId FROM dbo.Categories WHERE CategoryName = '5km Fun Run'),
        (SELECT UserId FROM dbo.Users WHERE Email = 'thabo.dlamini@example.com'), 'Confirmed'),
    ((SELECT CategoryId FROM dbo.Categories WHERE CategoryName = '10km Race'),
        (SELECT UserId FROM dbo.Users WHERE Email = 'aisha.patel@example.com'), 'Confirmed'),
    ((SELECT CategoryId FROM dbo.Categories WHERE CategoryName = '15km Trail'),
        (SELECT UserId FROM dbo.Users WHERE Email = 'thabo.dlamini@example.com'), 'Confirmed'),
    ((SELECT CategoryId FROM dbo.Categories WHERE CategoryName = 'Half Marathon'),
        (SELECT UserId FROM dbo.Users WHERE Email = 'aisha.patel@example.com'), 'Confirmed');
GO

-- Results for two of the enrolments above
INSERT INTO dbo.Results (EnrolmentId, FinishTime, Position, Status) VALUES
    ((SELECT EnrolmentId FROM dbo.Enrolments e
        JOIN dbo.Categories c ON e.CategoryId = c.CategoryId
        JOIN dbo.Users u ON e.ParticipantId = u.UserId
        WHERE c.CategoryName = '5km Fun Run' AND u.Email = 'thabo.dlamini@example.com'),
        '00:28:45', 12, 'Finished'),
    ((SELECT EnrolmentId FROM dbo.Enrolments e
        JOIN dbo.Categories c ON e.CategoryId = c.CategoryId
        JOIN dbo.Users u ON e.ParticipantId = u.UserId
        WHERE c.CategoryName = '10km Race' AND u.Email = 'aisha.patel@example.com'),
        '00:52:10', 5, 'Finished');
GO
