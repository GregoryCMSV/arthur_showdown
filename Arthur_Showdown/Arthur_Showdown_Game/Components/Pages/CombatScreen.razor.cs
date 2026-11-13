using Arthur_Showdown_Game.Components.Game;
using Arthur_Showdown_Game.Models;
using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Components.Pages
{
    public partial class CombatScreen : IDisposable
    {
        [Inject] public NavigationService NavService { get; set; } = default!;

        [Parameter] public int? GameId { get; set; }
        [Inject] public LoadingService _LoadingService { get; set; } = default!;
        [Inject] public HTTPService Http { get; set; } = default!;
        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Inject] public InputManager Input { get; set; } = default!;
        [Inject] public TurnManager Turnos { get; set; } = default!;
        [Inject] public SoundService Som { get; set; } = default!;

        private int _gameId;
        private int _vidaMax = 10;
        private int _ouro;
        private int _totalOuroGanho;
        private int _localId;
        private string _backgroundUrl = "https://cqzgfbtcmybebeqpomgv.supabase.co/storage/v1/object/public/game-assets/BG/BGFloresta.png";
        private int _posInicial = 1;
        private int _sessaoNumero = 1;
        private int _totalSessoes = 3;
        private int _waveNumero = 1;
        private int _totalWaves = 3;
        private int _tempoSegundos;
        private bool _pronto;
        private string? _erro;
        private bool _assinado;
        private PeriodicTimer? _timer;
        private CancellationTokenSource? _ctsTimer;

        private bool _escolhendoMelhoria;
        private bool _mostrarOrdemInimigos;
        private bool _processandoFimTurno;
        private TipoMelhoriaAtaque _melhoriaSorteada = TipoMelhoriaAtaque.MaisUmDano;
        private bool _escolhendoNovoAtaque;
        private List<AtaqueDto> _opcoesNovoAtaque = new();
        private List<SessaoPlanejadaDto> _planoSessoes = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (_PlayerService.CurrentCharacter == null)
                    await _PlayerService.LoadCharacters();

                var model = _PlayerService.CurrentCharacter;
                if (model?.Personagem == null) { _erro = "Nenhum personagem disponível."; return; }
                int? id = GameId;
                if (id == null)
                {
                    id = await Http.CriarGame(model.Personagem.Id);
                    if (id == null && int.TryParse(await Http.GetCurrentGame(), out var ativo)) id = ativo;
                }
                if (id == null) { _erro = "Não foi possível criar o jogo."; return; }
                _gameId = id.Value;

                var estado = await Http.GetEstado(_gameId, resetCooldown: true);
                if (estado == null) { _erro = "Não foi possível carregar o jogo."; return; }

                // Garante que todo o planejamento das sessoes e waves ja exista na memoria ao iniciar
                _planoSessoes = estado.PlanoSessoes != null && estado.PlanoSessoes.Count > 0
                    ? estado.PlanoSessoes
                    : GerarPlanoCompletoFallback(estado.Posicao);

                if (estado.TotalSessoes <= 0 || estado.Inimigos.Count <= 1)
                {
                    estado.SessaoNumero = estado.SessaoNumero > 0 ? estado.SessaoNumero : 1;
                    estado.TotalSessoes = _planoSessoes.Count;
                    estado.WaveNumero = estado.WaveNumero > 0 ? estado.WaveNumero : 1;
                    var sessaoPlan = _planoSessoes.FirstOrDefault(s => s.SessaoNumero == estado.SessaoNumero) ?? _planoSessoes.First();
                    estado.TotalWaves = sessaoPlan.Waves.Count;
                    estado.TamanhoMapa = sessaoPlan.TamanhoMapa;
                    var wavePlan = sessaoPlan.Waves.FirstOrDefault(w => w.WaveNumero == estado.WaveNumero) ?? sessaoPlan.Waves.First();
                    estado.Inimigos = RealocarInimigosAleatorios(wavePlan.Inimigos, estado.Posicao, sessaoPlan.TamanhoMapa, estado.SessaoNumero == _planoSessoes.Count);
                }

                AplicarEstado(model, estado);

                Turnos.OnMudou += AoMudar;
                Turnos.OnTurnoFinalizado += AoTurnoFinalizado;
                Input.AoSolicitarMovimento += AoMover;
                Input.AoSolicitarTrocaDirecao += AoVirar;
                Input.AoSolicitarInteracao += AoExecutarFila;
                Input.AoSolicitarEsperar += AoEsperar;
                _assinado = true;

                IniciarRelogio();
                _ = Som.IniciarMusicaAsync();
                _pronto = true;
            }
            catch (Exception ex)
            {
                _erro = $"Erro: {ex.Message}";
            }
            finally
            {
                _LoadingService.FinalizarCarregamento();
            }
        }

        private void IniciarRelogio()
        {
            _ctsTimer = new CancellationTokenSource();
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            _ = Task.Run(async () =>
            {
                try
                {
                    while (await _timer.WaitForNextTickAsync(_ctsTimer.Token))
                    {
                        if (Turnos.Fase != FaseTurno.Fim)
                        {
                            _tempoSegundos++;
                            await InvokeAsync(StateHasChanged);
                        }
                    }
                }
                catch { }
            });
        }

        private static string FormatarTempo(int seg)
            => $"{seg / 60:00}:{seg % 60:00}";

        private static string DescricaoMelhoria(TipoMelhoriaAtaque t) => t switch
        {
            TipoMelhoriaAtaque.MaisUmDano => "Melhoria sorteada: +1 de Dano no golpe selecionado",
            TipoMelhoriaAtaque.MaisDoisDanoMaisUmCooldown => "Melhoria sorteada: +2 de Dano e +1 de Cooldown no golpe selecionado",
            TipoMelhoriaAtaque.MenosUmCooldown => "Melhoria sorteada: -1 de Cooldown no golpe selecionado",
            TipoMelhoriaAtaque.AcaoRapida => "Melhoria sorteada: Transformar o golpe selecionado em Ação Rápida",
            TipoMelhoriaAtaque.EfeitoMaldicao => "Melhoria sorteada: Adicionar efeito de Maldição (dobro de dano no próximo ataque)",
            TipoMelhoriaAtaque.EfeitoEspelho => "Melhoria sorteada: Adicionar efeito de Espelho (vira o alvo para o lado contrário)",
            TipoMelhoriaAtaque.EfeitoEscudo => "Melhoria sorteada: Adicionar efeito de Escudo (ignora o primeiro dano recebido)",
            _ => ""
        };

        private static bool PodeReceberMelhoria(CartaAtaque c, TipoMelhoriaAtaque tipo) => tipo switch
        {
            TipoMelhoriaAtaque.EfeitoMaldicao => c.Ataque.EfeitoAdicionalId == null && !c.Ataque.PossuiEfeito(1),
            TipoMelhoriaAtaque.EfeitoEspelho => c.Ataque.EfeitoAdicionalId == null && !c.Ataque.PossuiEfeito(2),
            TipoMelhoriaAtaque.EfeitoEscudo => c.Ataque.EfeitoAdicionalId == null && !c.Ataque.PossuiEfeito(3),
            _ => true
        };

        private void AplicarEstado(CharacterModel model, EstadoJogoDto estado)
        {
            bool novaWaveOuSessao = _sessaoNumero != estado.SessaoNumero || _waveNumero != estado.WaveNumero || estado.Turno == 0;
            int tiles = estado.TamanhoMapa > 0 ? estado.TamanhoMapa : 5;
            _vidaMax = estado.VidaMaxima;
            _ouro = estado.OuroAtual;
            _totalOuroGanho = Math.Max(_totalOuroGanho, estado.TotalOuroGanho);
            _localId = estado.LocalAtualId;
            if (!string.IsNullOrWhiteSpace(estado.LocalImagemUrl))
                _backgroundUrl = estado.LocalImagemUrl;
            _posInicial = estado.Posicao;
            _sessaoNumero = estado.SessaoNumero > 0 ? estado.SessaoNumero : 1;
            _totalSessoes = estado.TotalSessoes > 0 ? estado.TotalSessoes : 3;
            _waveNumero = estado.WaveNumero > 0 ? estado.WaveNumero : 1;
            _totalWaves = estado.TotalWaves > 0 ? estado.TotalWaves : 3;
            _tempoSegundos = Math.Max(_tempoSegundos, estado.TempoSegundos);

            model.PosicaoPersonagem = estado.Posicao;
            model.DirecaoOlhar = estado.DirecaoOlhar == 0 ? 1 : estado.DirecaoOlhar;
            model.LimiteMapa = tiles;
            model.ModoCombate = true;

            var cartas = estado.Deck
                .Where(d => d.Ataque != null)
                .Select(d =>
                {
                    d.Ataque!.EfeitoPadraoId = d.Ataque.ObterEfeitoPadraoEfetivo();
                    d.Ataque.EfeitoAdicionalId ??= d.EfeitoId;
                    return new CartaAtaque
                    {
                        DeckAtaquesId = d.DeckAtaquesId,
                        CooldownRestante = d.CooldownRestante,
                        PosicaoFila = d.PosicaoFila,
                        QtdUsosNaRun = d.QtdUsosNaRun,
                        QtdVezesAdicionadoFila = d.QtdVezesAdicionadoFila,
                        QtdVezesRemovidoFila = d.QtdVezesRemovidoFila,
                        Ataque = d.Ataque!
                    };
                }).ToList();

            var inimigos = estado.Inimigos.Select(i =>
            {
                if (novaWaveOuSessao)
                {
                    i.Preparado = false;
                    i.Telegrafando = false;
                    i.RecuoPendente = false;
                    i.CursorAcao = 0;
                }
                return new InimigoEmCombate(i);
            }).ToList();

            Turnos.Iniciar(new BatalhaContexto
            {
                Jogador = model,
                VidaAtual = estado.VidaAtual,
                Turno = estado.Turno,
                TilesTotal = tiles,
                QtdMudancaDirecao = estado.QtdMudancaDirecao,
                QtdHitsSofridos = estado.QtdHitsSofridos,
                Inimigos = inimigos,
                PoolInimigosInvocacao = ObterPoolInvocacao(),
                TocarSomHit = ehFlecha => _ = Som.TocarHitAsync(ehFlecha),
                TocarSomMovimento = () => _ = Som.TocarMovimentoAsync()
            }, cartas);
        }

        private bool BloqueadoPorModal => _escolhendoMelhoria || _escolhendoNovoAtaque;

        private void AoMover(int dir)
        {
            if (BloqueadoPorModal) return;
            _ = Turnos.JogadorAgirAsync(new MoverAcao(dir));
        }

        private void AoVirar()
        {
            if (BloqueadoPorModal) return;
            _ = Turnos.JogadorAgirAsync(new VirarAcao());
        }

        private void AoExecutarFila()
        {
            if (BloqueadoPorModal) return;
            _ = Turnos.JogadorAgirAsync(new ExecutarFilaAcao(Turnos.Cartas));
        }

        private void AoEsperar()
        {
            if (BloqueadoPorModal) return;
            _ = Turnos.JogadorAgirAsync(new EsperarAcao());
        }

        private void AoMudar() => InvokeAsync(StateHasChanged);

        private void AoTurnoFinalizado()
        {
            if (_processandoFimTurno) return;
            try
            {
                _processandoFimTurno = true;

                if (Turnos.Ctx.VidaAtual <= 0)
                {
                    var snapDerrota = GerarSnapshot();
                    _ = Task.Run(async () =>
                    {
                        await Http.SalvarEstado(_gameId, snapDerrota);
                        await Http.FinalizarGame(_gameId, vitoria: false);
                    });
                    return;
                }

                bool limpouWave = Turnos.Ctx.Inimigos.Count == 0 && Turnos.Ctx.VidaAtual > 0;
                if (!limpouWave)
                {
                    // Salva em background sem travar a UI
                    var snapTurno = GerarSnapshot();
                    _ = Task.Run(() => Http.SalvarEstado(_gameId, snapTurno));
                    return;
                }

                if (_escolhendoMelhoria || _escolhendoNovoAtaque)
                    return;

                // Transicao instantanea usando o planejamento carregado no inicio da sessao
                var sessaoPlan = _planoSessoes.FirstOrDefault(s => s.SessaoNumero == _sessaoNumero);
                var wavePlanAtual = sessaoPlan?.Waves.FirstOrDefault(w => w.WaveNumero == _waveNumero);

                if (_waveNumero < _totalWaves)
                {
                    var possiveis = Enum.GetValues<TipoMelhoriaAtaque>()
                        .Where(m => Turnos.Cartas.Any(c => PodeReceberMelhoria(c, m)))
                        .ToArray();
                    if (possiveis.Length == 0)
                        possiveis = new[] { TipoMelhoriaAtaque.MaisUmDano };

                    _melhoriaSorteada = possiveis[Random.Shared.Next(possiveis.Length)];
                    _escolhendoMelhoria = true;
                    _ = InvokeAsync(StateHasChanged);
                }
                else if (_sessaoNumero < _totalSessoes)
                {
                    _opcoesNovoAtaque = sessaoPlan?.OpcoesNovoAtaqueAposSessao != null && sessaoPlan.OpcoesNovoAtaqueAposSessao.Count > 0
                        ? sessaoPlan.OpcoesNovoAtaqueAposSessao
                        : ObterOpcoesAtaqueFallback();
                    _escolhendoNovoAtaque = true;
                    _ = InvokeAsync(StateHasChanged);
                }
                else
                {
                    // Venceu a ultima wave da ultima sessao (Boss)
                    Turnos.FinalizarPartida();
                    var snapVitoria = GerarSnapshot(vitoria: true);
                    _ = Task.Run(async () =>
                    {
                        await Http.SalvarEstado(_gameId, snapVitoria);
                        await Http.FinalizarGame(_gameId, vitoria: true);
                    });
                    _ = InvokeAsync(StateHasChanged);
                }
            }
            catch { }
            finally
            {
                _processandoFimTurno = false;
            }
        }

        private async Task SelecionarMelhoria(CartaAtaque c)
        {
            _escolhendoMelhoria = false;
            AplicarMelhoriaNaCarta(c, _melhoriaSorteada);

            AvancarParaProximaWaveDoPlano(mudouSessao: false);
            Turnos.NotificarMudanca();
            await InvokeAsync(StateHasChanged);

            var snap = GerarSnapshot();
            int deckId = c.DeckAtaquesId;
            var tipo = _melhoriaSorteada;
            _ = Task.Run(async () =>
            {
                await Http.AplicarMelhoria(_gameId, deckId, tipo);
                await Http.SalvarEstado(_gameId, snap);
            });
        }

        private async Task SelecionarNovoAtaque(AtaqueDto atk)
        {
            _escolhendoNovoAtaque = false;
            Turnos.Cartas.Add(new CartaAtaque
            {
                DeckAtaquesId = Turnos.Cartas.Count + 1,
                CooldownRestante = 0,
                PosicaoFila = null,
                Ataque = new AtaqueDto
                {
                    Id = atk.Id,
                    Nome = atk.Nome,
                    Descricao = atk.Descricao,
                    Dano = atk.Dano,
                    Cooldown = atk.Cooldown,
                    IsFastAction = atk.IsFastAction,
                    LimiteAprimoramento = atk.LimiteAprimoramento,
                    EfeitoPadraoId = atk.ObterEfeitoPadraoEfetivo(),
                    EfeitoAdicionalId = atk.EfeitoAdicionalId,
                    HitboxJson = atk.HitboxJson,
                    ImagemUrl = atk.ImagemUrl
                }
            });

            AvancarParaProximaWaveDoPlano(mudouSessao: true);
            Turnos.NotificarMudanca();
            await InvokeAsync(StateHasChanged);

            var snap = GerarSnapshot();
            int atkId = atk.Id;
            _ = Task.Run(async () =>
            {
                await Http.EscolherNovoAtaque(_gameId, atkId);
                await Http.SalvarEstado(_gameId, snap);
            });
        }

        private void AvancarParaProximaWaveDoPlano(bool mudouSessao)
        {
            var model = _PlayerService.CurrentCharacter;
            if (model == null) return;

            if (mudouSessao)
            {
                _sessaoNumero++;
                _waveNumero = 1;
                foreach (var carta in Turnos.Cartas)
                {
                    carta.CooldownRestante = 0;
                    carta.PosicaoFila = null;
                }
            }
            else
            {
                _waveNumero++;
            }

            var sessaoPlan = _planoSessoes.FirstOrDefault(s => s.SessaoNumero == _sessaoNumero);
            int tamanho = sessaoPlan?.TamanhoMapa ?? (_sessaoNumero <= 1 ? 5 : 7);
            _totalWaves = sessaoPlan?.Waves.Count ?? (_sessaoNumero == 1 ? 3 : (_sessaoNumero == 2 ? 5 : 1));

            int posJog = Math.Clamp(model.PosicaoPersonagem, 0, tamanho - 1);
            model.PosicaoPersonagem = posJog;
            model.LimiteMapa = tamanho;

            var wavePlan = sessaoPlan?.Waves.FirstOrDefault(w => w.WaveNumero == _waveNumero);
            bool ehBoss = _sessaoNumero == _totalSessoes;

            var inimigosFonte = wavePlan?.Inimigos != null && wavePlan.Inimigos.Count > 0
                ? wavePlan.Inimigos
                : GerarWaveAleatoriaFallback(_sessaoNumero, _waveNumero, posJog, tamanho, ehBoss);

            var inimigosAlocados = RealocarInimigosAleatorios(inimigosFonte, posJog, tamanho, ehBoss);
            var inimigosCombate = inimigosAlocados.Select(i => new InimigoEmCombate(i)).ToList();

            Turnos.Iniciar(new BatalhaContexto
            {
                Jogador = model,
                VidaAtual = Turnos.Ctx.VidaAtual,
                Turno = Turnos.Ctx.Turno,
                TilesTotal = tamanho,
                QtdMudancaDirecao = Turnos.Ctx.QtdMudancaDirecao,
                QtdHitsSofridos = Turnos.Ctx.QtdHitsSofridos,
                EscudoAtivo = !mudouSessao && Turnos.Ctx.EscudoAtivo,
                Inimigos = inimigosCombate,
                PoolInimigosInvocacao = ObterPoolInvocacao(),
                TocarSomHit = ehFlecha => _ = Som.TocarHitAsync(ehFlecha),
                TocarSomMovimento = () => _ = Som.TocarMovimentoAsync()
            }, Turnos.Cartas);
        }

        private List<InimigoEstadoDto> ObterPoolInvocacao()
        {
            var pool = _planoSessoes
                .Where(s => s.SessaoNumero < _totalSessoes)
                .SelectMany(s => s.Waves)
                .SelectMany(w => w.Inimigos)
                .Where(i => !i.Nome.Contains("Tank", StringComparison.OrdinalIgnoreCase))
                .GroupBy(i => i.Nome)
                .Select(g => g.First())
                .ToList();

            string[] padrao = { "Esqueleto", "Arqueiro", "Lanceiro" };
            foreach (var nome in padrao)
            {
                if (!pool.Any(p => p.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase)))
                {
                    pool.Add(CriarInimigoFallback(nome, nome == "Lanceiro" ? 4 : 3, 0, 1));
                }
            }
            return pool;
        }

        private static List<InimigoEstadoDto> RealocarInimigosAleatorios(
            List<InimigoEstadoDto> planejados,
            int posJog,
            int tamanho,
            bool ehBoss)
        {
            if (ehBoss && planejados.Count > 0)
            {
                var b = planejados[0];
                int posBoss = posJog == tamanho - 1 ? 0 : tamanho - 1;
                int dirBoss = Math.Sign(posJog - posBoss);
                if (dirBoss == 0) dirBoss = -1;
                return new List<InimigoEstadoDto>
                {
                    new InimigoEstadoDto
                    {
                        PersonagemId = b.PersonagemId,
                        Nome = b.Nome,
                        ImagemUrl = b.ImagemUrl,
                        VidaAtual = b.VidaAtual,
                        Posicao = posBoss,
                        CursorAcao = 0,
                        Direcao = dirBoss,
                        Preparado = false,
                        Telegrafando = false,
                        RecuoPendente = false,
                        Acoes = b.Acoes
                    }
                };
            }

            var livres = Enumerable.Range(0, tamanho)
                .Where(t => t != posJog)
                .OrderBy(_ => Random.Shared.Next())
                .ToList();

            var resultado = new List<InimigoEstadoDto>();
            int total = Math.Min(planejados.Count, livres.Count);
            for (int i = 0; i < total; i++)
            {
                var src = planejados[i];
                resultado.Add(new InimigoEstadoDto
                {
                    PersonagemId = src.PersonagemId,
                    Nome = src.Nome,
                    ImagemUrl = src.ImagemUrl,
                    VidaAtual = src.VidaAtual,
                    Posicao = livres[i],
                    CursorAcao = 0,
                    Direcao = Random.Shared.Next(2) == 0 ? -1 : 1,
                    Preparado = false,
                    Telegrafando = false,
                    RecuoPendente = false,
                    Acoes = src.Acoes
                });
            }
            return resultado;
        }

        private static void AplicarMelhoriaNaCarta(CartaAtaque c, TipoMelhoriaAtaque tipo)
        {
            var novoAtaque = new AtaqueDto
            {
                Id = c.Ataque.Id,
                Nome = c.Ataque.Nome,
                Descricao = c.Ataque.Descricao,
                Dano = c.Ataque.Dano,
                Cooldown = c.Ataque.Cooldown,
                IsFastAction = c.Ataque.IsFastAction,
                LimiteAprimoramento = c.Ataque.LimiteAprimoramento,
                EfeitoPadraoId = c.Ataque.ObterEfeitoPadraoEfetivo(),
                EfeitoAdicionalId = c.Ataque.EfeitoAdicionalId,
                HitboxJson = c.Ataque.HitboxJson,
                ImagemUrl = c.Ataque.ImagemUrl
            };

            switch (tipo)
            {
                case TipoMelhoriaAtaque.MaisUmDano:
                    novoAtaque.Dano += 1;
                    break;
                case TipoMelhoriaAtaque.MaisDoisDanoMaisUmCooldown:
                    novoAtaque.Dano += 2;
                    novoAtaque.Cooldown += 1;
                    break;
                case TipoMelhoriaAtaque.MenosUmCooldown:
                    novoAtaque.Cooldown = Math.Max(0, novoAtaque.Cooldown - 1);
                    c.CooldownRestante = Math.Min(c.CooldownRestante, novoAtaque.Cooldown);
                    break;
                case TipoMelhoriaAtaque.AcaoRapida:
                    novoAtaque.IsFastAction = true;
                    break;
                case TipoMelhoriaAtaque.EfeitoMaldicao:
                    if (novoAtaque.EfeitoAdicionalId == null && !novoAtaque.PossuiEfeito(1))
                        novoAtaque.EfeitoAdicionalId = 1;
                    break;
                case TipoMelhoriaAtaque.EfeitoEspelho:
                    if (novoAtaque.EfeitoAdicionalId == null && !novoAtaque.PossuiEfeito(2))
                        novoAtaque.EfeitoAdicionalId = 2;
                    break;
                case TipoMelhoriaAtaque.EfeitoEscudo:
                    if (novoAtaque.EfeitoAdicionalId == null && !novoAtaque.PossuiEfeito(3))
                        novoAtaque.EfeitoAdicionalId = 3;
                    break;
            }

            c.Ataque = novoAtaque;
        }

        private static List<AtaqueDto> ObterOpcoesAtaqueFallback()
        {
            var todos = new List<AtaqueDto>
            {
                new AtaqueDto { Id = 8, Nome = "Ataque duplo", Descricao = "Ataca os espaços adjacentes", Dano = 2, Cooldown = 3, IsFastAction = false, HitboxJson = "{\"Origem\":0,\"Direcao\":2,\"AlcanceMin\":1,\"AlcanceMax\":1}", ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_swirl.png?16666f&20240924201144" },
                new AtaqueDto { Id = 13, Nome = "Raio", Descricao = "Dispara um raio no inimigo mais distante", Dano = 3, Cooldown = 4, IsFastAction = false, HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":99}", ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_lightning.png?a16c74&20240924201128" },
                new AtaqueDto { Id = 9, Nome = "Shuriken", Descricao = "Acerta o primeiro alvo a frente", Dano = 1, Cooldown = 3, IsFastAction = false, HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":99}", ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_shuriken.png?47f47b&20240924201137" },
                new AtaqueDto { Id = 12, Nome = "Bomba de fumaça", Descricao = "Troca de posição com o alvo a frente", Dano = 0, Cooldown = 5, IsFastAction = true, HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":1}", ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_smokeBomb.png?cac86d&20240924201139" },
                new AtaqueDto { Id = 7, Nome = "Bomba de fumaça costas", Descricao = "Troca de posição com o alvo atrás", Dano = 0, Cooldown = 5, IsFastAction = true, HitboxJson = "{\"Origem\":0,\"Direcao\":1,\"AlcanceMin\":1,\"AlcanceMax\":1}", ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_back_Smoke_Bom2.png?2bf901" },
                new AtaqueDto { Id = 10, Nome = "Amaldiçoar", Descricao = "Amaldiçoa o alvo a frente, fazendo ele receber o dobro de dano do próximo ataque", Dano = 0, Cooldown = 5, IsFastAction = true, EfeitoPadraoId = 1, HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":1}", ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_mark.png?411e2e&20240924201130" },
                new AtaqueDto { Id = 14, Nome = "Virar", Descricao = "Vira para o lado oposto", Dano = 0, Cooldown = 3, IsFastAction = true, HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":0,\"AlcanceMax\":0}", ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tile_Sharp_Turn.png?81ae49&20240924201515" },
                new AtaqueDto { Id = 15, Nome = "Ataque nas costas", Descricao = "Ataca o alvo diretamente atrás", Dano = 3, Cooldown = 3, IsFastAction = false, HitboxJson = "{\"Origem\":0,\"Direcao\":1,\"AlcanceMin\":1,\"AlcanceMax\":1}", ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_backStrike.png?80b1a6&20240924201109" },
                new AtaqueDto { Id = 11, Nome = "Espelho do lago", Descricao = "Vira todos os inimigos para o lado contrário", Dano = 0, Cooldown = 5, IsFastAction = true, EfeitoPadraoId = 2, HitboxJson = "{\"Origem\":0,\"Direcao\":2,\"AlcanceMin\":1,\"AlcanceMax\":99}", ImagemUrl = "https://shogunshowdown.wiki.gg/images/RainOfMirrors.png?71e858" }
            };
            return todos.OrderBy(_ => Random.Shared.Next()).Take(2).ToList();
        }

        private static List<SessaoPlanejadaDto> GerarPlanoCompletoFallback(int posJog)
        {
            var melhorias = Enum.GetValues<TipoMelhoriaAtaque>();
            var sessoes = new List<SessaoPlanejadaDto>();

            var configuracao = new (int sessao, int tamanho, int[] waves)[]
            {
                (1, 5, new[] { 2, 2, 3 }),
                (2, 7, new[] { 3, 3, 3, 4, 4 }),
                (3, 7, new[] { 1 })
            };

            foreach (var (sNum, tam, wavesQtd) in configuracao)
            {
                var listaWaves = new List<WavePlanejadaDto>();
                bool ehBoss = sNum == 3;
                for (int i = 0; i < wavesQtd.Length; i++)
                {
                    int wNum = i + 1;
                    listaWaves.Add(new WavePlanejadaDto
                    {
                        WaveNumero = wNum,
                        Inimigos = GerarWaveAleatoriaFallback(sNum, wNum, posJog, tam, ehBoss),
                        MelhoriaAposWave = wNum < wavesQtd.Length ? melhorias[Random.Shared.Next(melhorias.Length)] : null
                    });
                }

                sessoes.Add(new SessaoPlanejadaDto
                {
                    SessaoNumero = sNum,
                    TamanhoMapa = tam,
                    Waves = listaWaves,
                    OpcoesNovoAtaqueAposSessao = ObterOpcoesAtaqueFallback()
                });
            }

            return sessoes;
        }

        private static List<InimigoEstadoDto> GerarWaveAleatoriaFallback(int sessao, int wave, int posJog, int tamanho, bool ehBoss)
        {
            var inimigos = new List<InimigoEstadoDto>();
            if (ehBoss)
            {
                int posBoss = posJog == tamanho - 1 ? 0 : tamanho - 1;
                int dirBoss = Math.Sign(posJog - posBoss);
                if (dirBoss == 0) dirBoss = -1;
                inimigos.Add(CriarInimigoFallback("Tank", 20, posBoss, dirBoss));
                return inimigos;
            }

            int[] wavesS1 = { 2, 2, 3 };
            int[] wavesS2 = { 3, 3, 3, 4, 4 };
            int qtd = sessao == 1 ? wavesS1[Math.Clamp(wave - 1, 0, 2)] : wavesS2[Math.Clamp(wave - 1, 0, 4)];
            var livres = Enumerable.Range(0, tamanho)
                .Where(t => t != posJog)
                .OrderBy(_ => Random.Shared.Next())
                .ToList();
            string[] nomes = { "Esqueleto", "Arqueiro", "Lanceiro" };
            for (int i = 0; i < Math.Min(qtd, livres.Count); i++)
            {
                var nome = nomes[Random.Shared.Next(nomes.Length)];
                int vida = nome == "Lanceiro" ? 4 : 3;
                int dir = Random.Shared.Next(2) == 0 ? -1 : 1;
                inimigos.Add(CriarInimigoFallback(nome, vida, livres[i], dir));
            }
            return inimigos;
        }

        private static InimigoEstadoDto CriarInimigoFallback(string nome, int vida, int pos, int dir)
        {
            var (id, img, atkNome, dano, alcMax, recuoTipo) = nome switch
            {
                "Tank" => (2, "https://png.pngtree.com/png-clipart/20230703/ourmid/pngtree-battle-tank-png-image_7444190.png", "Golpe do Tank", 3, 1, ""),
                "Arqueiro" => (3, "https://thumbs.dreamstime.com/b/archer-fantasy-pixel-art-game-cartoon-character-elf-archer-fantasy-cartoon-character-video-game-sprite-pixel-art-style-413574152.jpg", "Flecha do Arqueiro", 2, 99, "RecuarSeProximo"),
                "Lanceiro" => (4, "https://i.pinimg.com/736x/fd/fe/11/fdfe11675f947ee2fe982472a3ba0f4d.jpg", "Estocada do Lanceiro", 2, 2, "RecuarAposAtaque"),
                _ => (1, "https://magicoloriage.com/wp-content/uploads/2025/06/Pixel-art-de-squelette-Minecraft-avec-arc.jpeg", "Golpe do Esqueleto", 2, 1, "RecuarAposAtaque")
            };

            var acoes = new List<InimigoAcaoDto>
            {
                new InimigoAcaoDto
                {
                    Ordem = 0,
                    Tipo = "Atacar",
                    Valor = dano,
                    Ataque = new AtaqueDto
                    {
                        Nome = atkNome,
                        ImagemUrl = img,
                        Dano = dano,
                        HitboxJson = $"{{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":{alcMax}}}"
                    }
                }
            };
            if (!string.IsNullOrEmpty(recuoTipo))
                acoes.Add(new InimigoAcaoDto { Ordem = 1, Tipo = recuoTipo, Valor = 1 });
            if (nome == "Tank")
                acoes.Add(new InimigoAcaoDto { Ordem = 1, Tipo = "InvocarAposAtaque", Valor = 2 });

            return new InimigoEstadoDto
            {
                PersonagemId = id,
                Nome = nome,
                ImagemUrl = img,
                VidaAtual = vida,
                Posicao = pos,
                CursorAcao = 0,
                Direcao = dir,
                Preparado = false,
                Telegrafando = false,
                RecuoPendente = false,
                Acoes = acoes
            };
        }

        private EstadoJogoDto GerarSnapshot(bool vitoria = false)
        {
            var ctx = Turnos.Ctx;
            return new EstadoJogoDto
            {
                GameId = _gameId,
                OuroAtual = _ouro,
                TotalOuroGanho = _totalOuroGanho,
                LocalAtualId = _localId,
                Turno = ctx.Turno,
                TempoSegundos = _tempoSegundos,
                QtdMudancaDirecao = ctx.QtdMudancaDirecao,
                QtdHitsSofridos = ctx.QtdHitsSofridos,
                VidaAtual = ctx.VidaAtual,
                VidaMaxima = _vidaMax,
                Posicao = ctx.Jogador.PosicaoPersonagem,
                DirecaoOlhar = ctx.Jogador.DirecaoOlhar,
                TamanhoMapa = ctx.TilesTotal,
                SessaoNumero = _sessaoNumero,
                TotalSessoes = _totalSessoes,
                WaveNumero = _waveNumero,
                TotalWaves = _totalWaves,
                Vitoria = vitoria,
                Deck = Turnos.Cartas.Select(c => new CartaEstadoDto
                {
                    DeckAtaquesId = c.DeckAtaquesId,
                    CooldownRestante = c.CooldownRestante,
                    PosicaoFila = c.PosicaoFila,
                    QtdUsosNaRun = c.QtdUsosNaRun,
                    QtdVezesAdicionadoFila = c.QtdVezesAdicionadoFila,
                    QtdVezesRemovidoFila = c.QtdVezesRemovidoFila,
                    EfeitoId = c.Ataque.EfeitoAdicionalId
                }).ToList(),
                Inimigos = ctx.Inimigos.Where(i => i.Vivo).Select(i => i.Estado).ToList()
            };
        }

        private void AlternarOrdemInimigos()
        {
            _mostrarOrdemInimigos = !_mostrarOrdemInimigos;
        }

        private void LimparEstadoLocalCombate()
        {
            foreach (var c in Turnos.Cartas)
            {
                c.PosicaoFila = null;
                c.CooldownRestante = 0;
            }
            if (_PlayerService.CurrentCharacter != null)
            {
                _PlayerService.CurrentCharacter.ModoCombate = false;
                _PlayerService.CurrentCharacter.LimiteMapa = 5;
            }
        }

        private void VoltarLobby()
        {
            LimparEstadoLocalCombate();
            NavService.GoToLobby();
        }

        private void VoltarMenuInicial()
        {
            LimparEstadoLocalCombate();
            NavService.GoToHome();
        }

        public void Dispose()
        {
            try { _ctsTimer?.Cancel(); _timer?.Dispose(); _ctsTimer?.Dispose(); } catch { }
            if (!_assinado) return;
            Turnos.OnMudou -= AoMudar;
            Turnos.OnTurnoFinalizado -= AoTurnoFinalizado;
            Input.AoSolicitarMovimento -= AoMover;
            Input.AoSolicitarTrocaDirecao -= AoVirar;
            Input.AoSolicitarInteracao -= AoExecutarFila;
            Input.AoSolicitarEsperar -= AoEsperar;
            if (_PlayerService.CurrentCharacter != null)
            {
                _PlayerService.CurrentCharacter.ModoCombate = false;
                _PlayerService.CurrentCharacter.LimiteMapa = 5;
            }
        }
    }
}
