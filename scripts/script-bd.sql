IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE TABLE [Itens] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Categoria] nvarchar(20) NOT NULL,
        [Unidade] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Itens] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE TABLE [PontosColeta] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(120) NOT NULL,
        [Cep] nvarchar(8) NOT NULL,
        [Logradouro] nvarchar(150) NOT NULL,
        [Numero] nvarchar(10) NULL,
        [Bairro] nvarchar(80) NOT NULL,
        [Cidade] nvarchar(80) NOT NULL,
        [Uf] nvarchar(2) NOT NULL,
        [Horario] nvarchar(100) NULL,
        [Ativo] bit NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        CONSTRAINT [PK_PontosColeta] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [SenhaHash] nvarchar(max) NOT NULL,
        [Papel] nvarchar(20) NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE TABLE [Necessidades] (
        [Id] int NOT NULL IDENTITY,
        [PontoColetaId] int NOT NULL,
        [ItemId] int NOT NULL,
        [QuantidadeNecessaria] int NOT NULL,
        [Urgencia] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        CONSTRAINT [PK_Necessidades] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Necessidades_Itens_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Itens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Necessidades_PontosColeta_PontoColetaId] FOREIGN KEY ([PontoColetaId]) REFERENCES [PontosColeta] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE TABLE [Doacoes] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioId] int NOT NULL,
        [NecessidadeId] int NOT NULL,
        [Quantidade] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CodigoEntrega] nvarchar(6) NOT NULL,
        [CriadoEm] datetime2 NOT NULL,
        [EntregueEm] datetime2 NULL,
        CONSTRAINT [PK_Doacoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Doacoes_Necessidades_NecessidadeId] FOREIGN KEY ([NecessidadeId]) REFERENCES [Necessidades] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Doacoes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Doacoes_CodigoEntrega] ON [Doacoes] ([CodigoEntrega]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Doacoes_NecessidadeId] ON [Doacoes] ([NecessidadeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Doacoes_UsuarioId] ON [Doacoes] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Itens_Nome] ON [Itens] ([Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Necessidades_ItemId] ON [Necessidades] ([ItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE INDEX [IX_Necessidades_PontoColetaId] ON [Necessidades] ([PontoColetaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110113204_CriacaoInicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261110113204_CriacaoInicial', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111015115_AdicionaIdempotencia'
)
BEGIN
    CREATE TABLE [IdempotencyKeys] (
        [Id] int NOT NULL IDENTITY,
        [Chave] nvarchar(100) NOT NULL,
        [UsuarioId] int NOT NULL,
        [Rota] nvarchar(100) NOT NULL,
        [HashRequisicao] nvarchar(64) NOT NULL,
        [StatusCode] int NOT NULL,
        [Resposta] nvarchar(max) NULL,
        [CriadoEm] datetime2 NOT NULL,
        CONSTRAINT [PK_IdempotencyKeys] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111015115_AdicionaIdempotencia'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IdempotencyKeys_UsuarioId_Chave] ON [IdempotencyKeys] ([UsuarioId], [Chave]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111015115_AdicionaIdempotencia'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111015115_AdicionaIdempotencia', N'10.0.14');
END;

COMMIT;
GO

