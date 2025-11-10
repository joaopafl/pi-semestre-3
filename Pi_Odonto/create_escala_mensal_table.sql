-- Script para criar a tabela de escala mensal dos dentistas
CREATE TABLE IF NOT EXISTS `escala_mensal_dentista` (
    `id_escala_mensal` int NOT NULL AUTO_INCREMENT,
    `id_dentista` int NOT NULL,
    `data_escala` date NOT NULL,
    `hora_inicio` time(6) NOT NULL,
    `hora_fim` time(6) NOT NULL,
    `ativo` tinyint(1) NOT NULL DEFAULT 1,
    `data_cadastro` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT `PK_escala_mensal_dentista` PRIMARY KEY (`id_escala_mensal`),
    CONSTRAINT `FK_escala_mensal_dentista_dentista_id_dentista` 
        FOREIGN KEY (`id_dentista`) REFERENCES `dentista` (`id_dentista`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- Criar índices para melhorar performance
CREATE INDEX IF NOT EXISTS `IX_escala_mensal_dentista_id_dentista` 
ON `escala_mensal_dentista` (`id_dentista`);

CREATE INDEX IF NOT EXISTS `IX_escala_mensal_dentista_data_escala` 
ON `escala_mensal_dentista` (`data_escala`);

CREATE INDEX IF NOT EXISTS `IX_escala_mensal_dentista_ativo` 
ON `escala_mensal_dentista` (`ativo`);

