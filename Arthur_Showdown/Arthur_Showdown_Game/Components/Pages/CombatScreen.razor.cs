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

        private int _gameId;
        private int _vidaMax = 10;
        private int _ouro;
        private int _totalOuroGanho;
        private int _localId;
        private int _posInicial = 1;
        private int _sessaoNumero = 1;
        private int _totalSessoes = 1;
        private int _waveNumero = 1;
        private int _totalWaves = 1;
        private int _tempoSegundos;
        private bool _pronto;
        private string? _erro;
        private bool _assinado;
        private PeriodicTimer? _timer;
        private CancellationTokenSource? _ctsTimer;

        private bool _escolhendoMelhoria;
        private bool _processandoFimTurno;
        private TipoMelhoriaAtaque _melhoriaSorteada = TipoMelhoriaAtaque.MaisUmDano;
        private bool _escolhendoNovoAtaque;
        private List<AtaqueDto> _opcoesNovoAtaque = new();
        private EstadoJogoDto? _estadoPendentePosRecompensa;

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

                // Ao iniciar combate ou voltar do login, o cooldown dos ataques vem sempre liberado
                var estado = await Http.GetEstado(_gameId, resetCooldown: true);
                if (estado == null) { _erro = "Não foi possível carregar o jogo."; return; }

                // Se a API conectada ainda nao tiver os campos de sessao/wave (ex.: servidor remoto desatualizado),
                // inicializa a estrutura local das 3 sessoes da Floresta para permitir a progressao completa
                if (estado.TotalSessoes <= 0)
                {
                    estado.SessaoNumero = 1;
                    estado.TotalSessoes = 3;
                    estado.WaveNumero = 1;
                    estado.TotalWaves = 3;
                    estado.TamanhoMapa = 5;
                    if (estado.Inimigos.Count <= 1)
                    {
                        var livres = Enumerable.Range(0, 5)
                            .Where(t => t != estado.Posicao)
                            .OrderByDescending(t => Math.Abs(t - estado.Posicao))
                            .ToList();
                        string[] nomes = { "Esqueleto", "Arqueiro", "Lanceiro" };
                        estado.Inimigos.Clear();
                        for (int i = 0; i < 2; i++)
                        {
                            var nome = nomes[Random.Shared.Next(nomes.Length)];
                            int vida = nome == "Lanceiro" ? 4 : 3;
                            int dir = Random.Shared.Next(2) == 0 ? -1 : 1;
                            estado.Inimigos.Add(CriarInimigoFallback(nome, vida, livres[i], dir));
                        }
                    }
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
            _ => ""
        };

        private void AplicarEstado(CharacterModel model, EstadoJogoDto estado)
        {
            bool novaWaveOuSessao = _sessaoNumero != estado.SessaoNumero || _waveNumero != estado.WaveNumero || estado.Turno == 0;
            int tiles = estado.TamanhoMapa > 0 ? estado.TamanhoMapa : 5;
            _vidaMax = estado.VidaMaxima;
            _ouro = estado.OuroAtual;
            _totalOuroGanho = Math.Max(_totalOuroGanho, estado.TotalOuroGanho);
            _localId = estado.LocalAtualId;
            _posInicial = estado.Posicao;
            _sessaoNumero = estado.SessaoNumero > 0 ? estado.SessaoNumero : 1;
            _totalSessoes = estado.TotalSessoes > 0 ? estado.TotalSessoes : 1;
            _waveNumero = estado.WaveNumero > 0 ? estado.WaveNumero : 1;
            _totalWaves = estado.TotalWaves > 0 ? estado.TotalWaves : 1;
            _tempoSegundos = Math.Max(_tempoSegundos, estado.TempoSegundos);

            model.PosicaoPersonagem = estado.Posicao;
            model.DirecaoOlhar = estado.DirecaoOlhar == 0 ? 1 : estado.DirecaoOlhar;
            model.LimiteMapa = tiles;
            model.ModoCombate = true;

            var cartas = estado.Deck
                .Where(d => d.Ataque != null)
                .Select(d => new CartaAtaque
                {
                    DeckAtaquesId = d.DeckAtaquesId,
                    CooldownRestante = d.CooldownRestante,
                    PosicaoFila = d.PosicaoFila,
                    QtdUsosNaRun = d.QtdUsosNaRun,
                    QtdVezesAdicionadoFila = d.QtdVezesAdicionadoFila,
                    QtdVezesRemovidoFila = d.QtdVezesRemovidoFila,
                    Ataque = d.Ataque!
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
                Inimigos = inimigos
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

        private async void AoTurnoFinalizado()
        {
            if (_processandoFimTurno) return;
            try
            {
                _processandoFimTurno = true;
                bool limpouWave = Turnos.Ctx.Inimigos.Count == 0 && Turnos.Ctx.VidaAtual > 0;
                int sessaoAntes = _sessaoNumero;

                if (Turnos.Ctx.VidaAtual <= 0)
                {
                    await Http.SalvarEstado(_gameId, GerarSnapshot());
                    await Http.FinalizarGame(_gameId, vitoria: false);
                    return;
                }

                // Se ja estamos aguardando escolha de recompensa, nao envia lista vazia de inimigos de novo
                if (limpouWave && (_escolhendoMelhoria || _escolhendoNovoAtaque || _estadoPendentePosRecompensa != null))
                    return;

                var novoEstado = await Http.SalvarEstado(_gameId, GerarSnapshot());

                if (!limpouWave) return;

                if (novoEstado == null || (novoEstado.Inimigos.Count == 0 && !novoEstado.Vitoria))
                {
                    // Fallback: se o servidor conectado for uma versao antiga que retorna 204 NoContent no PUT
                    // e nao gera waves automaticamente no banco, avanca a wave/sessao no cliente e persiste
                    novoEstado = AvancarWaveLocalmente();
                    await Http.SalvarEstado(_gameId, novoEstado);
                }

                if (novoEstado == null || _PlayerService.CurrentCharacter == null) return;

                if (novoEstado.Vitoria)
                {
                    AplicarEstado(_PlayerService.CurrentCharacter, novoEstado);
                    Turnos.FinalizarPartida();
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                _estadoPendentePosRecompensa = novoEstado;
                bool mudouSessao = novoEstado.SessaoNumero > sessaoAntes;

                if (mudouSessao)
                {
                    var opcoes = await Http.ObterOpcoesNovoAtaque(_gameId) ?? ObterOpcoesAtaqueFallback();
                    if (opcoes.Count > 0)
                    {
                        _opcoesNovoAtaque = opcoes;
                        _escolhendoNovoAtaque = true;
                        await InvokeAsync(StateHasChanged);
                        return;
                    }
                }
                else
                {
                    var valores = Enum.GetValues<TipoMelhoriaAtaque>();
                    _melhoriaSorteada = valores[Random.Shared.Next(valores.Length)];
                    _escolhendoMelhoria = true;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                _estadoPendentePosRecompensa = null;
                AplicarEstado(_PlayerService.CurrentCharacter, novoEstado);
                await InvokeAsync(StateHasChanged);
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
            var atualizado = await Http.AplicarMelhoria(_gameId, c.DeckAtaquesId, _melhoriaSorteada);
            if (atualizado == null)
            {
                AplicarMelhoriaLocal(c.DeckAtaquesId, _melhoriaSorteada);
                atualizado = _estadoPendentePosRecompensa;
            }
            _estadoPendentePosRecompensa = null;

            if (atualizado != null && _PlayerService.CurrentCharacter != null)
                AplicarEstado(_PlayerService.CurrentCharacter, atualizado);

            await InvokeAsync(StateHasChanged);
        }

        private async Task SelecionarNovoAtaque(AtaqueDto atk)
        {
            _escolhendoNovoAtaque = false;
            var atualizado = await Http.EscolherNovoAtaque(_gameId, atk.Id);
            if (atualizado == null && _estadoPendentePosRecompensa != null)
            {
                _estadoPendentePosRecompensa.Deck.Add(new CartaEstadoDto
                {
                    DeckAtaquesId = _estadoPendentePosRecompensa.Deck.Count + 1,
                    CooldownRestante = 0,
                    PosicaoFila = null,
                    Ataque = atk
                });
                atualizado = _estadoPendentePosRecompensa;
            }
            _estadoPendentePosRecompensa = null;

            if (atualizado != null && _PlayerService.CurrentCharacter != null)
                AplicarEstado(_PlayerService.CurrentCharacter, atualizado);

            await InvokeAsync(StateHasChanged);
        }

        private void AplicarMelhoriaLocal(int deckAtaquesId, TipoMelhoriaAtaque tipo)
        {
            var c = _estadoPendentePosRecompensa?.Deck.FirstOrDefault(d => d.DeckAtaquesId == deckAtaquesId);
            if (c?.Ataque == null) return;
            switch (tipo)
            {
                case TipoMelhoriaAtaque.MaisUmDano:
                    c.Ataque.Dano += 1;
                    break;
                case TipoMelhoriaAtaque.MaisDoisDanoMaisUmCooldown:
                    c.Ataque.Dano += 2;
                    c.Ataque.Cooldown += 1;
                    break;
                case TipoMelhoriaAtaque.MenosUmCooldown:
                    c.Ataque.Cooldown = Math.Max(0, c.Ataque.Cooldown - 1);
                    break;
                case TipoMelhoriaAtaque.AcaoRapida:
                    c.Ataque.IsFastAction = true;
                    break;
            }
        }

        private static List<AtaqueDto> ObterOpcoesAtaqueFallback() => new()
        {
            new AtaqueDto { Id = 101, Nome = "Estocada Rápida", Descricao = "Ataque de 2 células à frente", Dano = 2, Cooldown = 1, IsFastAction = true, HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":2}" },
            new AtaqueDto { Id = 102, Nome = "Golpe Pesado", Descricao = "Golpe devastador a 1 célula", Dano = 4, Cooldown = 2, IsFastAction = false, HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":1}" }
        };

        private EstadoJogoDto AvancarWaveLocalmente()
        {
            int sessao = _sessaoNumero;
            int totalSessoes = _totalSessoes > 0 ? _totalSessoes : 3;
            int wave = _waveNumero + 1;
            int totalWaves = sessao == 1 ? 3 : (sessao == 2 ? 5 : 1);
            bool mudouSessao = false;

            if (wave > totalWaves)
            {
                sessao++;
                wave = 1;
                mudouSessao = true;
            }

            bool vitoria = sessao > totalSessoes;
            int tamanho = sessao <= 1 ? 5 : 7;
            totalWaves = sessao == 1 ? 3 : (sessao == 2 ? 5 : 1);

            var deckDto = Turnos.Cartas.Select(c => new CartaEstadoDto
            {
                DeckAtaquesId = c.DeckAtaquesId,
                CooldownRestante = mudouSessao ? 0 : c.CooldownRestante,
                PosicaoFila = mudouSessao ? null : c.PosicaoFila,
                QtdUsosNaRun = c.QtdUsosNaRun,
                QtdVezesAdicionadoFila = c.QtdVezesAdicionadoFila,
                QtdVezesRemovidoFila = c.QtdVezesRemovidoFila,
                Ataque = c.Ataque
            }).ToList();

            int posJog = Math.Clamp(Turnos.Ctx.Jogador.PosicaoPersonagem, 0, tamanho - 1);
            var inimigos = new List<InimigoEstadoDto>();

            if (!vitoria)
            {
                if (sessao == totalSessoes)
                {
                    int posBoss = posJog == tamanho - 1 ? 0 : tamanho - 1;
                    int dirBoss = Math.Sign(posJog - posBoss);
                    if (dirBoss == 0) dirBoss = -1;
                    inimigos.Add(CriarInimigoFallback("Tank", 20, posBoss, dirBoss));
                }
                else
                {
                    int[] wavesS1 = { 2, 2, 3 };
                    int[] wavesS2 = { 3, 3, 3, 4, 4 };
                    int qtd = sessao == 1 ? wavesS1[Math.Clamp(wave - 1, 0, 2)] : wavesS2[Math.Clamp(wave - 1, 0, 4)];
                    var livres = Enumerable.Range(0, tamanho)
                        .Where(t => t != posJog)
                        .OrderByDescending(t => Math.Abs(t - posJog))
                        .ToList();
                    string[] nomes = { "Esqueleto", "Arqueiro", "Lanceiro" };
                    for (int i = 0; i < Math.Min(qtd, livres.Count); i++)
                    {
                        var nome = nomes[Random.Shared.Next(nomes.Length)];
                        int vida = nome == "Lanceiro" ? 4 : 3;
                        int dir = Random.Shared.Next(2) == 0 ? -1 : 1;
                        inimigos.Add(CriarInimigoFallback(nome, vida, livres[i], dir));
                    }
                }
            }

            return new EstadoJogoDto
            {
                GameId = _gameId,
                OuroAtual = _ouro,
                TotalOuroGanho = _totalOuroGanho,
                LocalAtualId = _localId,
                Turno = Turnos.Ctx.Turno,
                TempoSegundos = _tempoSegundos,
                QtdMudancaDirecao = Turnos.Ctx.QtdMudancaDirecao,
                QtdHitsSofridos = Turnos.Ctx.QtdHitsSofridos,
                VidaAtual = Turnos.Ctx.VidaAtual,
                VidaMaxima = _vidaMax,
                Posicao = posJog,
                DirecaoOlhar = Turnos.Ctx.Jogador.DirecaoOlhar,
                TamanhoMapa = tamanho,
                SessaoNumero = Math.Min(sessao, totalSessoes),
                TotalSessoes = totalSessoes,
                WaveNumero = wave,
                TotalWaves = totalWaves,
                Vitoria = vitoria,
                Deck = deckDto,
                Inimigos = inimigos
            };
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

        private EstadoJogoDto GerarSnapshot()
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
                Deck = Turnos.Cartas.Select(c => new CartaEstadoDto
                {
                    DeckAtaquesId = c.DeckAtaquesId,
                    CooldownRestante = c.CooldownRestante,
                    PosicaoFila = c.PosicaoFila,
                    QtdUsosNaRun = c.QtdUsosNaRun,
                    QtdVezesAdicionadoFila = c.QtdVezesAdicionadoFila,
                    QtdVezesRemovidoFila = c.QtdVezesRemovidoFila
                }).ToList(),
                Inimigos = ctx.Inimigos.Where(i => i.Vivo).Select(i => i.Estado).ToList()
            };
        }

        private void VoltarLobby()
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
            NavService.GoToLobby();
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
