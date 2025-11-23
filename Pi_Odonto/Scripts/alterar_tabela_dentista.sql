-- Script para adicionar os campos 'motivacao' e 'situacao' na tabela dentista
-- Execute este script no seu banco de dados MySQL

USE seu_banco_de_dados; -- Substitua pelo nome do seu banco de dados

-- Adicionar coluna 'motivacao' (opcional, pode ser NULL)
ALTER TABLE dentista 
ADD COLUMN motivacao VARCHAR(1000) NULL 
COMMENT 'Motivação do dentista ao se cadastrar como voluntário';

-- Adicionar coluna 'situacao' (obrigatória, com valor padrão 'candidato')
ALTER TABLE dentista 
ADD COLUMN situacao VARCHAR(50) NOT NULL DEFAULT 'candidato' 
COMMENT 'Situação do dentista: candidato, contratado, banco de talento, desvinculado';

-- Atualizar todos os registros existentes para terem situação 'candidato' se estiverem NULL
UPDATE dentista 
SET situacao = 'candidato' 
WHERE situacao IS NULL OR situacao = '';

-- Definir todos os dentistas existentes como inativos (conforme requisito)
-- ATENÇÃO: Descomente a linha abaixo apenas se desejar atualizar os registros existentes
-- UPDATE dentista SET ativo = 0;

-- Verificar a estrutura da tabela após as alterações
DESCRIBE dentista;

