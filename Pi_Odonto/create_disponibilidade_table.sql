-- Script para criar a tabela de disponibilidade do dentista
CREATE TABLE IF NOT EXISTS `disponibilidade_dentista` (
    `id_disponibilidade` int NOT NULL AUTO_INCREMENT,
    `id_dentista` int NOT NULL,
    `dia_semana` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `hora_inicio` time(6) NOT NULL,
    `hora_fim` time(6) NOT NULL,
    `ativo` tinyint(1) NOT NULL DEFAULT 1,
    `data_cadastro` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT `PK_disponibilidade_dentista` PRIMARY KEY (`id_disponibilidade`),
    CONSTRAINT `FK_disponibilidade_dentista_dentista_id_dentista` 
        FOREIGN KEY (`id_dentista`) REFERENCES `dentista` (`id_dentista`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- Criar índice para melhorar performance
CREATE INDEX IF NOT EXISTS `IX_disponibilidade_dentista_id_dentista` 
ON `disponibilidade_dentista` (`id_dentista`);

-- Marcar as migrations como aplicadas no histórico
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250830231334_AdicionarRecuperacaoSenhaToken', '8.0.5');

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250922002618_AdicionarEscalaTrabalho', '8.0.5');

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250922004214_AdicionarDisponibilidadeDentistaNovo', '8.0.5');
