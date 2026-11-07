using Aluguel.Application.Abstractions;
using Aluguel.Application.Relatorios;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Fiscal;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

public sealed class RelatorioRepository(AppDbContext db) : IRelatorioRepository
{
    private static readonly string[] Meses =
        ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];

    public async Task<RelatorioPainelDto> ObterPainelAsync(
        Guid? clienteId,
        Guid? imovelId,
        int? ano,
        int? mes,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        int? diasAtrasoMinimos,
        DateOnly hoje,
        CancellationToken ct = default)
    {
        var (inicio, fim) = ResolverPeriodo(ano, mes, dataInicio, dataFim, hoje);
        var filtro = new RelatorioFiltroDto(ano, mes, inicio, fim, clienteId, imovelId, diasAtrasoMinimos);

        var nfsesQ = db.NotasFiscais.AsNoTracking().AsQueryable();
        if (clienteId is { } cid) nfsesQ = nfsesQ.Where(n => n.ClienteId == cid);
        if (imovelId is { } iid) nfsesQ = nfsesQ.Where(n => n.ImovelId == iid);

        var nfses = await nfsesQ.ToListAsync(ct);
        var boletos = await db.Boletos.AsNoTracking()
            .Where(b => clienteId == null || b.ClienteId == clienteId)
            .Where(b => imovelId == null || b.ImovelId == imovelId)
            .ToListAsync(ct);
        var pagamentos = await db.Pagamentos.AsNoTracking()
            .Where(p => clienteId == null || p.ClienteId == clienteId)
            .Where(p => imovelId == null || p.ImovelId == imovelId)
            .ToListAsync(ct);

        var boletoPorNfse = boletos.Where(b => b.NfseId is not null)
            .GroupBy(b => b.NfseId!.Value).ToDictionary(g => g.Key, g => g.First());
        var pagPorNfse = pagamentos.Where(p => p.NfseId is not null)
            .GroupBy(p => p.NfseId!.Value).ToDictionary(g => g.Key, g => g.First());

        var nfsesPeriodo = nfses
            .Where(n => n.Status != StatusNfse.Cancelada)
            .Where(n => CompetenciaNoPeriodo(n.Competencia, inicio, fim))
            .ToList();

        var imoveisQ = db.Imoveis.AsNoTracking().AsQueryable();
        if (clienteId is { } cid2) imoveisQ = imoveisQ.Where(i => i.ClienteId == cid2);
        if (imovelId is { } iid2) imoveisQ = imoveisQ.Where(i => i.Id == iid2);
        var imoveis = await imoveisQ.ToListAsync(ct);
        var imovelNomes = imoveis.ToDictionary(i => i.Id, i => i.Nome);

        var contratosQ = db.Contratos.AsNoTracking().AsQueryable();
        if (clienteId is { } cid3) contratosQ = contratosQ.Where(c => c.ClienteId == cid3);
        if (imovelId is { } iid3) contratosQ = contratosQ.Where(c => c.ImovelId == iid3);
        var contratos = await contratosQ.ToListAsync(ct);
        var ativos = contratos.Where(c => c.Status == StatusContrato.Ativo).ToList();
        var imoveisComContratoAtivo = ativos.Select(c => c.ImovelId).ToHashSet();

        var inquilinoIds = nfsesPeriodo.Select(n => n.InquilinoId).Concat(ativos.Select(c => (Guid?)c.InquilinoId))
            .Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        var inquilinos = await db.Inquilinos.AsNoTracking()
            .Where(i => inquilinoIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Nome })
            .ToDictionaryAsync(i => i.Id, i => i.Nome, ct);

        var clientesIds = nfsesPeriodo.Select(n => n.ClienteId).Concat(imoveis.Select(i => i.ClienteId)).Distinct().ToList();
        var clientes = await db.Clientes.AsNoTracking()
            .Where(c => clientesIds.Contains(c.Id))
            .Select(c => new { c.Id, Nome = c.Nome ?? c.RazaoSocial ?? "—" })
            .ToDictionaryAsync(c => c.Id, c => c.Nome, ct);

        // —— Dashboard / rendimento ——
        decimal SomaAluguel() => nfsesPeriodo.Sum(n => n.ValorAluguel);
        decimal SomaIptu() => nfsesPeriodo.Sum(n => n.ValorIptu);
        decimal SomaCondo() => nfsesPeriodo.Sum(n => n.ValorCondominio);
        decimal SomaMulta() => nfsesPeriodo.Sum(n => n.Multa);
        decimal SomaJuros() => nfsesPeriodo.Sum(n => n.Juros);
        var receitaTotal = nfsesPeriodo.Sum(n => n.ValorFaturado);
        var totalRecebido = nfsesPeriodo
            .Where(n => pagPorNfse.ContainsKey(n.Id))
            .Sum(n => pagPorNfse[n.Id].ValorPago);
        var emAbertoNfses = nfsesPeriodo.Where(n => !pagPorNfse.ContainsKey(n.Id)).ToList();
        var totalEmAberto = emAbertoNfses.Sum(n => n.ValorFaturado);
        var inadPct = receitaTotal > 0 ? Math.Round(totalEmAberto / receitaTotal * 100m, 2) : 0m;

        var ocupados = imoveis.Count(i => i.Status == Domain.Common.StatusAtivoInativo.Ativo && imoveisComContratoAtivo.Contains(i.Id));
        var ativosCadastro = imoveis.Count(i => i.Status == Domain.Common.StatusAtivoInativo.Ativo);
        var vagos = Math.Max(0, ativosCadastro - ocupados);

        var evolucao = MontarEvolucaoMensal(nfsesPeriodo, inicio, fim);

        // Aging
        var aging = MontarAging(emAbertoNfses, boletoPorNfse, hoje);

        // Concentração top 5 contratos (por valor faturado no período)
        var porContrato = nfsesPeriodo
            .Where(n => n.ContratoId is not null)
            .GroupBy(n => n.ContratoId!.Value)
            .Select(g => g.Sum(x => x.ValorFaturado))
            .OrderByDescending(v => v)
            .Take(5)
            .Sum();
        var concentracao = receitaTotal > 0 ? Math.Round(porContrato / receitaTotal * 100m, 2) : 0m;

        var dashboard = new DashboardRelatorioDto(
            receitaTotal,
            SomaAluguel(),
            SomaIptu(),
            SomaCondo(),
            totalRecebido,
            totalEmAberto,
            inadPct,
            ativos.Count,
            ocupados,
            vagos,
            evolucao,
            aging,
            concentracao);

        var rendimento = new RendimentoPeriodoDto(
            SomaAluguel(), SomaIptu(), SomaCondo(), SomaMulta(), SomaJuros(),
            SomaAluguel() + SomaIptu() + SomaCondo() + SomaMulta() + SomaJuros(),
            evolucao);

        // —— Geradas x recebidas ——
        var porCompGerado = nfsesPeriodo
            .GroupBy(n => n.Competencia)
            .OrderBy(g => ChaveOrdenacaoCompetencia(g.Key))
            .Select(g =>
            {
                var gerado = g.Sum(x => x.ValorFaturado);
                var recebido = g.Where(x => pagPorNfse.ContainsKey(x.Id)).Sum(x => pagPorNfse[x.Id].ValorPago);
                return new SerieGeradoRecebidoDto(g.Key, RotuloCompetencia(g.Key), gerado, recebido);
            }).ToList();

        var geradas = receitaTotal;
        var pagas = totalRecebido;
        var taxa = geradas > 0 ? Math.Round(pagas / geradas * 100m, 2) : 0m;
        var cobrancas = new CobrancasGeradasRecebidasDto(geradas, pagas, totalEmAberto, taxa, porCompGerado);

        // —— Inadimplentes ——
        var minDias = diasAtrasoMinimos ?? 1;
        var inadItens = new List<LinhaInadimplenteDto>();
        foreach (var n in emAbertoNfses)
        {
            if (!boletoPorNfse.TryGetValue(n.Id, out var bol)) continue;
            var dias = hoje.DayNumber - bol.DataVencimento.DayNumber;
            if (dias < minDias) continue;
            var inqNome = n.InquilinoId is { } iidn && inquilinos.TryGetValue(iidn, out var nome)
                ? nome : "—";
            inadItens.Add(new LinhaInadimplenteDto(
                n.Id, inqNome, imovelNomes.GetValueOrDefault(n.ImovelId, "—"), n.ImovelId,
                n.ValorFaturado, dias, n.Competencia, bol.DataVencimento));
        }
        inadItens = inadItens.OrderByDescending(i => i.DiasAtraso).ToList();
        var totalInad = inadItens.Sum(i => i.Valor);
        var inad = new InadimplentesDto(
            totalInad,
            receitaTotal > 0 ? Math.Round(totalInad / receitaTotal * 100m, 2) : 0m,
            inadItens.Select(i => i.InquilinoNome).Distinct().Count(),
            inadItens);

        // —— Ranking atrasos (histórico: pagos atrasados + em aberto) ——
        var atrasosPorImovel = new Dictionary<Guid, List<int>>();
        foreach (var n in nfses.Where(x => x.Status != StatusNfse.Cancelada))
        {
            if (!boletoPorNfse.TryGetValue(n.Id, out var bol)) continue;
            DateOnly dataRef;
            if (pagPorNfse.TryGetValue(n.Id, out var pag))
                dataRef = pag.DataPagamento;
            else
                dataRef = hoje;
            var dias = dataRef.DayNumber - bol.DataVencimento.DayNumber;
            if (dias <= 0) continue;
            if (!atrasosPorImovel.TryGetValue(n.ImovelId, out var lista))
            {
                lista = [];
                atrasosPorImovel[n.ImovelId] = lista;
            }
            lista.Add(dias);
        }
        var ranking = atrasosPorImovel
            .Select(kv => new RankingAtrasoDto(
                kv.Key,
                imovelNomes.GetValueOrDefault(kv.Key, "—"),
                Math.Round((decimal)kv.Value.Average(), 1),
                kv.Value.Count))
            .OrderByDescending(r => r.MediaDiasAtraso)
            .ToList();

        // —— Fluxo de caixa previsto (próximos 3 meses) ——
        var fluxo = new List<FluxoCaixaMesDto>();
        for (var i = 0; i < 3; i++)
        {
            var refMes = hoje.AddMonths(i);
            var comp = $"{refMes.Month:D2}/{refMes.Year}";
            // Cobranças já emitidas sem pagamento com vencimento no mês + contratos ativos sem cobrança na competência
            var emitidasMes = nfses
                .Where(n => n.Status != StatusNfse.Cancelada && n.Competencia == comp && !pagPorNfse.ContainsKey(n.Id))
                .Sum(n => n.ValorFaturado);
            var imoveisJaCobrados = nfses
                .Where(n => n.Status != StatusNfse.Cancelada && n.Competencia == comp)
                .Select(n => n.ImovelId).ToHashSet();
            var previsaoContratos = ativos
                .Where(c => !imoveisJaCobrados.Contains(c.ImovelId))
                .Where(c => imovelId == null || c.ImovelId == imovelId)
                .Sum(c => c.ValorAluguel);
            fluxo.Add(new FluxoCaixaMesDto(comp, $"{Meses[refMes.Month - 1]}/{refMes.Year % 100:D2}",
                emitidasMes + previsaoContratos));
        }

        // —— Rentabilidade por imóvel ——
        var rent = nfsesPeriodo
            .GroupBy(n => n.ImovelId)
            .Select(g =>
            {
                var comps = g.Select(x => x.Competencia).Distinct().Count();
                var receita = g.Sum(x => x.ValorFaturado);
                return new RentabilidadeImovelDto(
                    g.Key,
                    imovelNomes.GetValueOrDefault(g.Key, "—"),
                    receita,
                    comps > 0 ? Math.Round(receita / comps, 2) : 0m,
                    comps);
            })
            .OrderByDescending(r => r.ReceitaPeriodo)
            .ToList();

        // —— Vacância ——
        var vagosDetalhe = new List<VacanciaImovelDto>();
        foreach (var im in imoveis.Where(i => i.Status == Domain.Common.StatusAtivoInativo.Ativo && !imoveisComContratoAtivo.Contains(i.Id)))
        {
            var ultimo = contratos
                .Where(c => c.ImovelId == im.Id && c.Status != StatusContrato.Cancelado)
                .OrderByDescending(c => c.DataFimPrevista ?? c.DataInicio)
                .FirstOrDefault();
            DateOnly desde;
            if (ultimo?.DataFimPrevista is { } fimContrato)
                desde = fimContrato;
            else if (ultimo is not null)
                desde = ultimo.DataInicio;
            else
                desde = DateOnly.FromDateTime(im.CreatedAt.UtcDateTime);
            var diasVago = Math.Max(0, hoje.DayNumber - desde.DayNumber);
            vagosDetalhe.Add(new VacanciaImovelDto(im.Id, im.Nome, diasVago, desde));
        }
        vagosDetalhe = vagosDetalhe.OrderByDescending(v => v.DiasVago).ToList();
        var vacancia = new VacanciaDto(
            ativosCadastro, ocupados, vagos,
            ativosCadastro > 0 ? Math.Round((decimal)vagos / ativosCadastro * 100m, 2) : 0m,
            vagosDetalhe);

        // —— Contratos vencendo ——
        var vencendo = ativos
            .Where(c => c.DataFimPrevista is not null)
            .Select(c =>
            {
                var fimC = c.DataFimPrevista!.Value;
                var dias = fimC.DayNumber - hoje.DayNumber;
                return new { Contrato = c, Dias = dias, Fim = fimC };
            })
            .Where(x => x.Dias >= 0 && x.Dias <= 90)
            .OrderBy(x => x.Dias)
            .Select(x => new ContratoVencendoDto(
                x.Contrato.Id,
                inquilinos.GetValueOrDefault(x.Contrato.InquilinoId, "—"),
                imovelNomes.GetValueOrDefault(x.Contrato.ImovelId, "—"),
                x.Fim,
                x.Dias))
            .ToList();

        // —— Fiscal ——
        var emitidas = nfsesPeriodo.Count(n => n.Status == StatusNfse.Emitida);
        var canceladasPeriodo = nfses
            .Where(n => n.Status == StatusNfse.Cancelada && CompetenciaNoPeriodo(n.Competencia, inicio, fim))
            .ToList();
        var fiscalPorComp = nfsesPeriodo
            .Where(n => n.Status == StatusNfse.Emitida)
            .GroupBy(n => n.Competencia)
            .OrderBy(g => ChaveOrdenacaoCompetencia(g.Key))
            .Select(g => new CompetenciaValorDto(g.Key, RotuloCompetencia(g.Key), g.Sum(x => x.ValorFaturado), g.Count()))
            .ToList();
        var notasSemPagamento = nfsesPeriodo.Count(n => n.Status == StatusNfse.Emitida && !pagPorNfse.ContainsKey(n.Id));
        // Cobranças (boletos) sem NFS-e emitida: rascunhos / rejeitadas no período
        var cobSemNota = nfsesPeriodo.Count(n => n.Status is StatusNfse.Rascunho or StatusNfse.Rejeitada);
        var fiscal = new FiscalNfseDto(
            emitidas,
            canceladasPeriodo.Count,
            nfsesPeriodo.Where(n => n.Status == StatusNfse.Emitida).Sum(n => n.ValorFaturado),
            fiscalPorComp,
            notasSemPagamento,
            cobSemNota);

        // —— Proprietários (clientes) ——
        var props = nfsesPeriodo
            .GroupBy(n => n.ClienteId)
            .Select(g =>
            {
                var rec = g.Sum(x => x.ValorFaturado);
                return new ProprietarioReceitaDto(
                    g.Key,
                    clientes.GetValueOrDefault(g.Key, "—"),
                    rec,
                    receitaTotal > 0 ? Math.Round(rec / receitaTotal * 100m, 2) : 0m);
            })
            .OrderByDescending(p => p.Receita)
            .ToList();

        var dre = new DreSimplificadoDto(
            SomaAluguel(), SomaMulta(), SomaJuros(), SomaIptu(), SomaCondo(),
            SomaAluguel() + SomaMulta() + SomaJuros() + SomaIptu() + SomaCondo());

        // —— Score inquilino ——
        var scores = new List<ScoreInquilinoDto>();
        var atrasosPorInq = new Dictionary<Guid, List<int>>();
        foreach (var n in nfses.Where(x => x.Status != StatusNfse.Cancelada && x.InquilinoId is not null))
        {
            if (!boletoPorNfse.TryGetValue(n.Id, out var bol)) continue;
            DateOnly dataRef = pagPorNfse.TryGetValue(n.Id, out var pag) ? pag.DataPagamento : hoje;
            var dias = dataRef.DayNumber - bol.DataVencimento.DayNumber;
            if (dias <= 0) continue;
            var inqId = n.InquilinoId!.Value;
            if (!atrasosPorInq.TryGetValue(inqId, out var lista))
            {
                lista = [];
                atrasosPorInq[inqId] = lista;
            }
            lista.Add(dias);
        }
        foreach (var kv in atrasosPorInq)
        {
            var media = (decimal)kv.Value.Average();
            var risco = media switch
            {
                >= 60 => "Alto",
                >= 30 => "Médio",
                _ => "Baixo",
            };
            scores.Add(new ScoreInquilinoDto(
                kv.Key,
                inquilinos.GetValueOrDefault(kv.Key, "—"),
                kv.Value.Count,
                Math.Round(media, 1),
                risco));
        }
        scores = scores.OrderByDescending(s => s.MediaDiasAtraso).ToList();

        return new RelatorioPainelDto(
            filtro, dashboard, rendimento, cobrancas, inad, ranking, fluxo, rent,
            vacancia, vencendo, fiscal, props, dre, scores);
    }

    private static (DateOnly Inicio, DateOnly Fim) ResolverPeriodo(
        int? ano, int? mes, DateOnly? dataInicio, DateOnly? dataFim, DateOnly hoje)
    {
        if (dataInicio is not null || dataFim is not null)
        {
            var i = dataInicio ?? new DateOnly(2000, 1, 1);
            var f = dataFim ?? hoje;
            return (i, f);
        }

        if (ano is { } a && mes is { } m && m is >= 1 and <= 12)
        {
            var i = new DateOnly(a, m, 1);
            return (i, i.AddMonths(1).AddDays(-1));
        }

        if (ano is { } a2)
            return (new DateOnly(a2, 1, 1), new DateOnly(a2, 12, 31));

        // Padrão: ano corrente
        return (new DateOnly(hoje.Year, 1, 1), new DateOnly(hoje.Year, 12, 31));
    }

    private static bool CompetenciaNoPeriodo(string competencia, DateOnly inicio, DateOnly fim)
    {
        if (!TryParseCompetencia(competencia, out var mes, out var ano)) return false;
        var compInicio = new DateOnly(ano, mes, 1);
        var compFim = compInicio.AddMonths(1).AddDays(-1);
        return compInicio <= fim && compFim >= inicio;
    }

    private static bool TryParseCompetencia(string competencia, out int mes, out int ano)
    {
        mes = 0; ano = 0;
        if (string.IsNullOrWhiteSpace(competencia) || competencia.Length < 7) return false;
        return int.TryParse(competencia.AsSpan(0, 2), out mes)
               && int.TryParse(competencia.AsSpan(3, 4), out ano)
               && mes is >= 1 and <= 12;
    }

    private static string ChaveOrdenacaoCompetencia(string competencia) =>
        TryParseCompetencia(competencia, out var m, out var a) ? $"{a:D4}-{m:D2}" : competencia;

    private static string RotuloCompetencia(string competencia) =>
        TryParseCompetencia(competencia, out var m, out var a) ? $"{Meses[m - 1]}/{a % 100:D2}" : competencia;

    private static List<SerieMensalDto> MontarEvolucaoMensal(
        List<Domain.Fiscal.NotaFiscalServico> nfses, DateOnly inicio, DateOnly fim)
    {
        var mapa = nfses.GroupBy(n => n.Competencia)
            .ToDictionary(g => g.Key, g => g.ToList());

        var series = new List<SerieMensalDto>();
        var cursor = new DateOnly(inicio.Year, inicio.Month, 1);
        var limite = new DateOnly(fim.Year, fim.Month, 1);
        // Cap 24 months to keep charts readable
        for (var i = 0; i < 24 && cursor <= limite; i++)
        {
            var comp = $"{cursor.Month:D2}/{cursor.Year}";
            mapa.TryGetValue(comp, out var lista);
            lista ??= [];
            series.Add(new SerieMensalDto(
                comp,
                RotuloCompetencia(comp),
                lista.Sum(x => x.ValorAluguel),
                lista.Sum(x => x.ValorIptu),
                lista.Sum(x => x.ValorCondominio),
                lista.Sum(x => x.ValorFaturado)));
            cursor = cursor.AddMonths(1);
        }
        return series;
    }

    private static List<AgingFaixaDto> MontarAging(
        List<Domain.Fiscal.NotaFiscalServico> emAberto,
        Dictionary<Guid, Domain.Financeiro.Boleto> boletos,
        DateOnly hoje)
    {
        var faixas = new (string Nome, int Min, int Max)[]
        {
            ("0–30 dias", 0, 30),
            ("31–60 dias", 31, 60),
            ("61–90 dias", 61, 90),
            ("+90 dias", 91, int.MaxValue),
        };
        var buckets = faixas.ToDictionary(f => f.Nome, _ => (Qtd: 0, Valor: 0m));
        foreach (var n in emAberto)
        {
            if (!boletos.TryGetValue(n.Id, out var bol)) continue;
            var dias = Math.Max(0, hoje.DayNumber - bol.DataVencimento.DayNumber);
            var faixa = faixas.First(f => dias >= f.Min && dias <= f.Max).Nome;
            var atual = buckets[faixa];
            buckets[faixa] = (atual.Qtd + 1, atual.Valor + n.ValorFaturado);
        }
        return faixas.Select(f => new AgingFaixaDto(f.Nome, buckets[f.Nome].Qtd, buckets[f.Nome].Valor)).ToList();
    }
}
