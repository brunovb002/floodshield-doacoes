namespace FloodShield.Doacoes.Api.Models;

public enum Papel
{
    Cidadao,
    Gestor
}

public enum CategoriaItem
{
    Agua,
    Alimento,
    Higiene,
    Limpeza,
    Roupa,
    Pet,
    Outros
}

public enum UnidadeMedida
{
    Unidade,
    Litro,
    Quilo,
    Pacote,
    Caixa
}

public enum Urgencia
{
    Baixa,
    Media,
    Alta,
    Critica
}

public enum StatusNecessidade
{
    Aberta,
    Atendida,
    Cancelada
}

public enum StatusDoacao
{
    Prometida,
    Entregue,
    Cancelada
}
