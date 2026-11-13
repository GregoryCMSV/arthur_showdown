using Arthur_Showdown_Game.Components.Game;
using Arthur_Showdown_Game.Models;
using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Components.Pages;

public partial class TutorialScreen : ComponentBase, IDisposable
{
    private enum EtapaTutorial
    {
        IntroVida,
        MoverAteCentro,
        AdicionarNaFila,
        AproximarEsqueleto,
        AtacarEsqueleto,
        ExplicarTurnos,
        VirarParaArqueiro,
        CaminharAteArqueiro,
        DerrotarArqueiro,
        MatarPrimeiroDosDois,
        EsperarCooldown,
        DerrotarUltimo,
        DerrotarLanceiro,
        Concluido
    }

    [Inject] public NavigationService NavService { get; set; } = default!;
    [Inject] public LoadingService _LoadingService { get; set; } = default!;
    [Inject] public PlayerService _PlayerService { get; set; } = default!;
    [Inject] public InputManager Input { get; set; } = default!;
    [Inject] public TurnManager Turnos { get; set; } = default!;
    [Inject] public SoundService Som { get; set; } = default!;

    private readonly string _backgroundUrl = "https://cqzgfbtcmybebeqpomgv.supabase.co/storage/v1/object/public/game-assets/BG/BGFloresta.png";
    private const int _vidaMax = 10;
    private const int _posInicial = 0;

    private bool _pronto;
    private bool _assinado;
    private bool _transicionando;
    private bool _tiroArqueiroDisparado;
    private EtapaTutorial EtapaAtual { get; set; } = EtapaTutorial.IntroVida;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (_PlayerService.CurrentCharacter == null)
                await _PlayerService.LoadCharacters();

            _PlayerService.GarantirPersonagemTutorial();
            var model = _PlayerService.CurrentCharacter!;
            model.PosicaoPersonagem = _posInicial;
            model.DirecaoOlhar = 1;
            model.LimiteMapa = 5;
            model.ModoCombate = true;

            var cartas = PrepararDeckTutorial(model);

            Turnos.Iniciar(new BatalhaContexto
            {
                Jogador = model,
                VidaAtual = _vidaMax,
                Turno = 0,
                TilesTotal = 5,
                Inimigos = new List<InimigoEmCombate>(),
                IgnorarFimSemInimigos = true,
                ImpedirMorteJogador = true,
                PlanejadorInimigosCustom = PlanejarInimigosTutorial,
                TocarSomHit = ehFlecha => _ = Som.TocarHitAsync(ehFlecha),
                TocarSomMovimento = () => _ = Som.TocarMovimentoAsync()
            }, cartas);

            Turnos.OnMudou += AoMudar;
            Turnos.OnAcaoExecutada += AoAcaoExecutada;
            Input.AoSolicitarMovimento += AoMover;
            Input.AoSolicitarTrocaDirecao += AoVirar;
            Input.AoSolicitarInteracao += AoExecutarFila;
            Input.AoSolicitarEsperar += AoEsperar;
            _assinado = true;

            _ = Som.IniciarMusicaAsync();
            _pronto = true;
        }
        finally
        {
            _LoadingService.FinalizarCarregamento();
        }
    }

    private static List<CartaAtaque> PrepararDeckTutorial(CharacterModel model)
    {
        var baseAtaques = model.GetAtaques() ?? new List<AtaqueDto>();
        var clones = baseAtaques.Select(a => new AtaqueDto
        {
            Id = a.Id,
            Nome = a.Nome,
            Descricao = a.Descricao,
            Dano = a.Dano,
            Cooldown = a.Cooldown,
            IsFastAction = a.IsFastAction,
            LimiteAprimoramento = a.LimiteAprimoramento,
            EfeitoPadraoId = a.ObterEfeitoPadraoEfetivo(),
            EfeitoAdicionalId = a.EfeitoAdicionalId,
            HitboxJson = a.HitboxJson,
            ImagemUrl = a.ImagemUrl
        }).ToList();

        if (!clones.Any(a => a.Dano >= 2))
        {
            clones.Insert(0, new AtaqueDto
            {
                Id = 1,
                Nome = "Espada",
                Descricao = "Causa 2 de dano no espaço à frente",
                Dano = 2,
                Cooldown = 2,
                IsFastAction = false,
                HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":1}",
                ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_katana.png"
            });
        }

        if (clones.Count < 2)
        {
            clones.Add(new AtaqueDto
            {
                Id = 2,
                Nome = "Escudo",
                Descricao = "Causa 1 de dano e concede um escudo",
                Dano = 1,
                Cooldown = 3,
                IsFastAction = true,
                EfeitoPadraoId = 3,
                HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":1}",
                ImagemUrl = ""
            });
        }

        return clones.Select(a => new CartaAtaque
        {
            Ataque = a,
            CooldownRestante = 0,
            PosicaoFila = null
        }).ToList();
    }

    private static InimigoEmCombate CriarEsqueleto(int pos, int dir, int vida = 2) => new(new InimigoEstadoDto
    {
        PersonagemId = 1,
        Nome = "Esqueleto",
        ImagemUrl = "https://cqzgfbtcmybebeqpomgv.supabase.co/storage/v1/object/public/game-assets/Personagens/Enemy/Esqueleto.png",
        VidaAtual = vida,
        Posicao = pos,
        CursorAcao = 0,
        Direcao = dir,
        Preparado = false,
        Telegrafando = false,
        RecuoPendente = false,
        Acoes = new List<InimigoAcaoDto>
        {
            new()
            {
                Ordem = 0,
                Tipo = "Atacar",
                Valor = 2,
                Ataque = new AtaqueDto
                {
                    Nome = "Golpe do Esqueleto",
                    ImagemUrl = "https://cqzgfbtcmybebeqpomgv.supabase.co/storage/v1/object/public/game-assets/Ataques/Espada.png",
                    Dano = 2,
                    HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":1}"
                }
            }
        }
    });

    private static InimigoEmCombate CriarArqueiro(int pos, int dir) => new(new InimigoEstadoDto
    {
        PersonagemId = 3,
        Nome = "Arqueiro",
        ImagemUrl = "https://cqzgfbtcmybebeqpomgv.supabase.co/storage/v1/object/public/game-assets/Personagens/Enemy/Arqueiro.png",
        VidaAtual = 2,
        Posicao = pos,
        CursorAcao = 0,
        Direcao = dir,
        Preparado = false,
        Telegrafando = false,
        RecuoPendente = false,
        Acoes = new List<InimigoAcaoDto>
        {
            new()
            {
                Ordem = 0,
                Tipo = "Atacar",
                Valor = 2,
                Ataque = new AtaqueDto
                {
                    Nome = "Flecha",
                    ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_arrow.png?bab938&20240924201111",
                    Dano = 2,
                    HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":99}"
                }
            },
            new()
            {
                Ordem = 1,
                Tipo = "RecuarSeProximo",
                Valor = 1
            }
        }
    });

    private static InimigoEmCombate CriarLanceiro(int pos, int dir) => new(new InimigoEstadoDto
    {
        PersonagemId = 4,
        Nome = "Lanceiro",
        ImagemUrl = "https://cqzgfbtcmybebeqpomgv.supabase.co/storage/v1/object/public/game-assets/Personagens/Enemy/Lanceiro.png",
        VidaAtual = 4,
        Posicao = pos,
        CursorAcao = 0,
        Direcao = dir,
        Preparado = false,
        Telegrafando = false,
        RecuoPendente = false,
        Acoes = new List<InimigoAcaoDto>
        {
            new()
            {
                Ordem = 0,
                Tipo = "Atacar",
                Valor = 2,
                Ataque = new AtaqueDto
                {
                    Nome = "Estocada do Lanceiro",
                    ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_spear.png?d69678&20240924201140",
                    Dano = 2,
                    HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":2}"
                }
            },
            new()
            {
                Ordem = 1,
                Tipo = "RecuarAposAtaque",
                Valor = 1
            }
        }
    });

    private void PlanejarInimigosTutorial(List<InimigoEmCombate> inimigos, int posJogador, int tilesTotal)
    {
        var vivos = inimigos.Where(i => i.Vivo).ToList();
        for (int idx = 0; idx < vivos.Count; idx++)
            vivos[idx].OrdemTurno = idx + 1;

        switch (EtapaAtual)
        {
            case EtapaTutorial.MoverAteCentro:
            case EtapaTutorial.AdicionarNaFila:
                foreach (var ini in vivos)
                {
                    ini.DirecaoIntencao = InimigoIA.Rumo(ini, posJogador);
                    ini.IntencaoPlanejada = !ini.Preparado ? DecisaoInimigo.Preparar : DecisaoInimigo.Esperar;
                }
                break;

            case EtapaTutorial.AproximarEsqueleto:
                foreach (var ini in vivos)
                {
                    ini.DirecaoIntencao = InimigoIA.Rumo(ini, posJogador);
                    ini.IntencaoPlanejada = DecisaoInimigo.Telegrafar;
                }
                break;

            case EtapaTutorial.VirarParaArqueiro:
                foreach (var ini in vivos)
                {
                    ini.DirecaoIntencao = InimigoIA.Rumo(ini, posJogador);
                    ini.IntencaoPlanejada = DecisaoInimigo.Esperar;
                }
                break;

            case EtapaTutorial.CaminharAteArqueiro:
                foreach (var ini in vivos)
                {
                    ini.DirecaoIntencao = InimigoIA.Rumo(ini, posJogador);
                    if (ini.Telegrafando)
                        ini.IntencaoPlanejada = DecisaoInimigo.Atacar;
                    else if (posJogador >= 4)
                        ini.IntencaoPlanejada = DecisaoInimigo.Telegrafar; 
                    else
                        ini.IntencaoPlanejada = DecisaoInimigo.Esperar;
                }
                break;

            default:
                InimigoIA.PlanejarTurno(inimigos, posJogador, tilesTotal);
                break;
        }
    }

    private void AvancarEtapaInformativa()
    {
        if (EtapaAtual == EtapaTutorial.IntroVida)
        {
            Turnos.Ctx.Inimigos = new List<InimigoEmCombate> { CriarEsqueleto(pos: 4, dir: -1) };
            EtapaAtual = EtapaTutorial.MoverAteCentro;
            Turnos.PlanejarProximoTurno();
            Turnos.Ctx.RegistrarRaiosInvocacao(new[] { 4 });
            StateHasChanged();
        }
        else if (EtapaAtual == EtapaTutorial.ExplicarTurnos)
        {
            IniciarEtapaArqueiro();
            StateHasChanged();
        }
    }

    private void AoMudar() => InvokeAsync(StateHasChanged);

    private async void AoAcaoExecutada(IAcaoDeTurno acao)
    {
        if (_transicionando || !_pronto || Turnos.Ctx == null) return;
        int pos = Turnos.Ctx.Jogador.PosicaoPersonagem;
        int dir = Turnos.Ctx.Jogador.DirecaoOlhar;
        bool temFila = Turnos.Cartas.Any(c => c.EmFila);

        switch (EtapaAtual)
        {
            case EtapaTutorial.MoverAteCentro:
                if (pos == 2)
                {
                    EtapaAtual = EtapaTutorial.AdicionarNaFila;
                    Turnos.PlanejarProximoTurno();
                }
                break;

            case EtapaTutorial.AdicionarNaFila:
                if (temFila)
                {
                    EtapaAtual = EtapaTutorial.AproximarEsqueleto;
                    Turnos.PlanejarProximoTurno();
                }
                break;

            case EtapaTutorial.AproximarEsqueleto:
                if (pos == 3)
                {
                    EtapaAtual = EtapaTutorial.AtacarEsqueleto;
                    Turnos.PlanejarProximoTurno();
                }
                break;

            case EtapaTutorial.AtacarEsqueleto:
                if (!Turnos.Ctx.Inimigos.Any(i => i.Vivo))
                {
                    _transicionando = true;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(350);
                    EtapaAtual = EtapaTutorial.ExplicarTurnos;
                    _transicionando = false;
                }
                break;

            case EtapaTutorial.VirarParaArqueiro:
                if (dir == -1)
                {
                    EtapaAtual = EtapaTutorial.CaminharAteArqueiro;
                    Turnos.PlanejarProximoTurno();
                }
                break;

            case EtapaTutorial.CaminharAteArqueiro:
                if (pos <= 2 && _tiroArqueiroDisparado)
                {
                    EtapaAtual = EtapaTutorial.DerrotarArqueiro;
                    Turnos.PlanejarProximoTurno();
                }
                else if (pos <= 2)
                {
                    _tiroArqueiroDisparado = true;
                    EtapaAtual = EtapaTutorial.DerrotarArqueiro;
                    Turnos.PlanejarProximoTurno();
                }
                break;

            case EtapaTutorial.DerrotarArqueiro:
                if (!Turnos.Ctx.Inimigos.Any(i => i.Vivo))
                {
                    _transicionando = true;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(350);
                    IniciarEtapaDoisInimigos();
                    _transicionando = false;
                }
                break;

            case EtapaTutorial.MatarPrimeiroDosDois:
                int vivos = Turnos.Ctx.Inimigos.Count(i => i.Vivo);
                if (vivos == 0)
                {
                    _transicionando = true;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(350);
                    IniciarEtapaLanceiro();
                    _transicionando = false;
                }
                else if (vivos == 1)
                {
                    EtapaAtual = EtapaTutorial.EsperarCooldown;
                    Turnos.PlanejarProximoTurno();
                }
                break;

            case EtapaTutorial.EsperarCooldown:
                if (!Turnos.Ctx.Inimigos.Any(i => i.Vivo))
                {
                    _transicionando = true;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(350);
                    IniciarEtapaLanceiro();
                    _transicionando = false;
                }
                else if (acao is EsperarAcao)
                {
                    EtapaAtual = EtapaTutorial.DerrotarUltimo;
                }
                break;

            case EtapaTutorial.DerrotarUltimo:
                if (!Turnos.Ctx.Inimigos.Any(i => i.Vivo))
                {
                    _transicionando = true;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(350);
                    IniciarEtapaLanceiro();
                    _transicionando = false;
                }
                break;

            case EtapaTutorial.DerrotarLanceiro:
                if (!Turnos.Ctx.Inimigos.Any(i => i.Vivo))
                {
                    EtapaAtual = EtapaTutorial.Concluido;
                }
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private void IniciarEtapaArqueiro()
    {
        if (Turnos.Ctx == null) return;
        _tiroArqueiroDisparado = false;
        Turnos.Ctx.Jogador.PosicaoPersonagem = 4;
        Turnos.Ctx.Jogador.DirecaoOlhar = 1;
        Turnos.Ctx.EscudoAtivo = false;
        Turnos.Ctx.Inimigos = new List<InimigoEmCombate> { CriarArqueiro(pos: 0, dir: 1) };
        EtapaAtual = EtapaTutorial.VirarParaArqueiro;
        Turnos.PlanejarProximoTurno();
        Turnos.Ctx.RegistrarRaiosInvocacao(new[] { 0 });
    }

    private void IniciarEtapaDoisInimigos()
    {
        if (Turnos.Ctx == null) return;
        Turnos.Ctx.Jogador.PosicaoPersonagem = 1;
        Turnos.Ctx.Jogador.DirecaoOlhar = -1;
        Turnos.Ctx.EscudoAtivo = false;
        foreach (var c in Turnos.Cartas)
        {
            c.CooldownRestante = 0;
            c.PosicaoFila = null;
        }
        Turnos.Ctx.Inimigos = new List<InimigoEmCombate>
        {
            CriarEsqueleto(pos: 0, dir: 1),
            CriarEsqueleto(pos: 4, dir: -1, 3)
        };
        EtapaAtual = EtapaTutorial.MatarPrimeiroDosDois;
        Turnos.PlanejarProximoTurno();
        Turnos.Ctx.RegistrarRaiosInvocacao(new[] { 0, 4 });
    }

    private void IniciarEtapaLanceiro()
    {
        if (Turnos.Ctx == null) return;
        Turnos.Ctx.Jogador.PosicaoPersonagem = Math.Min(Turnos.Ctx.Jogador.PosicaoPersonagem, 2);
        Turnos.Ctx.Jogador.DirecaoOlhar = 1;
        Turnos.Ctx.EscudoAtivo = false;

        foreach (var c in Turnos.Cartas)
        {
            c.CooldownRestante = 0;
            c.PosicaoFila = null;
        }

        if (!Turnos.Cartas.Any(c => c.Ataque.Nome.Equals("Amaldiçoar", StringComparison.OrdinalIgnoreCase)))
        {
            Turnos.Cartas.Add(new CartaAtaque
            {
                CooldownRestante = 0,
                PosicaoFila = null,
                Ataque = new AtaqueDto
                {
                    Id = 10,
                    Nome = "Amaldiçoar",
                    Descricao = "Amaldiçoa o alvo a frente, fazendo ele receber o dobro de dano do próximo ataque",
                    Dano = 0,
                    Cooldown = 5,
                    IsFastAction = true,
                    EfeitoPadraoId = 1,
                    HitboxJson = "{\"Origem\":0,\"Direcao\":0,\"AlcanceMin\":1,\"AlcanceMax\":1}",
                    ImagemUrl = "https://shogunshowdown.wiki.gg/images/Tiles_mark.png?411e2e&20240924201130"
                }
            });
        }

        Turnos.Ctx.Inimigos = new List<InimigoEmCombate> { CriarLanceiro(pos: 4, dir: -1) };
        EtapaAtual = EtapaTutorial.DerrotarLanceiro;
        Turnos.PlanejarProximoTurno();
        Turnos.Ctx.RegistrarRaiosInvocacao(new[] { 4 });
    }

    private bool MostrarBarraAtaques => EtapaAtual >= EtapaTutorial.AdicionarNaFila && EtapaAtual != EtapaTutorial.Concluido;

    private bool BloquearAtaques => _transicionando
                                   || EtapaAtual is EtapaTutorial.IntroVida
                                                or EtapaTutorial.MoverAteCentro
                                                or EtapaTutorial.AproximarEsqueleto
                                                or EtapaTutorial.ExplicarTurnos
                                                or EtapaTutorial.VirarParaArqueiro
                                                or EtapaTutorial.CaminharAteArqueiro
                                                or EtapaTutorial.Concluido;

    private bool DestacarBarraAtaques => EtapaAtual == EtapaTutorial.AdicionarNaFila
                                      || (EtapaAtual == EtapaTutorial.DerrotarLanceiro && Turnos.Cartas.Count(c => c.EmFila) < 2);

    private bool MostrarBotaoMovimento => EtapaAtual is not (EtapaTutorial.IntroVida or EtapaTutorial.ExplicarTurnos or EtapaTutorial.Concluido);

    private bool MostrarBotoesAcao => EtapaAtual >= EtapaTutorial.AtacarEsqueleto
                                    && EtapaAtual is not (EtapaTutorial.ExplicarTurnos or EtapaTutorial.Concluido);

    private bool PodeMover(int dir)
    {
        if (_transicionando || !_pronto || Turnos.Ctx == null) return false;
        int pos = Turnos.Ctx.Jogador.PosicaoPersonagem;
        if (pos + dir < 0 || pos + dir >= Turnos.Ctx.TilesTotal) return false;
        if (Turnos.Ctx.Inimigos.Any(i => i.Vivo && i.Estado.Posicao == pos + dir)) return false;

        return EtapaAtual switch
        {
            EtapaTutorial.IntroVida => false,
            EtapaTutorial.MoverAteCentro => dir == 1 && pos < 2,
            EtapaTutorial.AdicionarNaFila => false,
            EtapaTutorial.AproximarEsqueleto => dir == 1 && pos < 3,
            EtapaTutorial.AtacarEsqueleto => false,
            EtapaTutorial.ExplicarTurnos => false,
            EtapaTutorial.VirarParaArqueiro => false,
            EtapaTutorial.CaminharAteArqueiro => dir == -1 && pos > 2,
            EtapaTutorial.EsperarCooldown => false,
            EtapaTutorial.Concluido => false,
            _ => true
        };
    }

    private bool PodeVirar()
    {
        if (_transicionando || !_pronto) return false;
        return EtapaAtual switch
        {
            EtapaTutorial.VirarParaArqueiro => true,
            EtapaTutorial.DerrotarArqueiro => true,
            EtapaTutorial.MatarPrimeiroDosDois => true,
            EtapaTutorial.DerrotarUltimo => true,
            EtapaTutorial.DerrotarLanceiro => true,
            _ => false
        };
    }

    private bool PodeEsperar()
    {
        if (_transicionando || !_pronto) return false;
        return EtapaAtual switch
        {
            EtapaTutorial.DerrotarArqueiro => true,
            EtapaTutorial.MatarPrimeiroDosDois => true,
            EtapaTutorial.EsperarCooldown => true,
            EtapaTutorial.DerrotarUltimo => true,
            EtapaTutorial.DerrotarLanceiro => true,
            _ => false
        };
    }

    private bool PodeAtacar()
    {
        if (_transicionando || !_pronto) return false;
        if (!Turnos.Cartas.Any(c => c.EmFila)) return false;
        return EtapaAtual switch
        {
            EtapaTutorial.AtacarEsqueleto => true,
            EtapaTutorial.DerrotarArqueiro => true,
            EtapaTutorial.MatarPrimeiroDosDois => true,
            EtapaTutorial.DerrotarUltimo => true,
            EtapaTutorial.DerrotarLanceiro => true,
            _ => false
        };
    }

    private string? BotaoDestacado => EtapaAtual switch
    {
        EtapaTutorial.MoverAteCentro => "direita",
        EtapaTutorial.AproximarEsqueleto => "direita",
        EtapaTutorial.AtacarEsqueleto => Turnos.Cartas.Any(c => c.EmFila) ? "atacar" : null,
        EtapaTutorial.VirarParaArqueiro => "virar",
        EtapaTutorial.CaminharAteArqueiro => "esquerda",
        EtapaTutorial.EsperarCooldown => "esperar",
        _ => null
    };

    private string TituloEtapa => EtapaAtual switch
    {
        EtapaTutorial.IntroVida => "Vida",
        EtapaTutorial.MoverAteCentro => "Movimento",
        EtapaTutorial.AdicionarNaFila => "Fila de Ataques",
        EtapaTutorial.AproximarEsqueleto => "Aproximação",
        EtapaTutorial.AtacarEsqueleto => "Atacar!",
        EtapaTutorial.ExplicarTurnos => "Turnos e Ações",
        EtapaTutorial.VirarParaArqueiro => "Virar para o Inimigo",
        EtapaTutorial.CaminharAteArqueiro => "Ataque à Distância",
        EtapaTutorial.DerrotarArqueiro => "Contra-ataque",
        EtapaTutorial.MatarPrimeiroDosDois => "Combate com 2 Inimigos",
        EtapaTutorial.EsperarCooldown => "Recuperar Cooldown",
        EtapaTutorial.DerrotarUltimo => "Último Esqueleto",
        EtapaTutorial.DerrotarLanceiro => "Efeitos, Ação Rápida e Fila",
        _ => "Tutorial"
    };

    private string MensagemEtapa => EtapaAtual switch
    {
        EtapaTutorial.IntroVida =>
            "Este é o seu personagem, no topo da tela fica a sua Vida (❤ 10 / 10). Fique atento para não deixá-la chegar a zero.",
        EtapaTutorial.MoverAteCentro =>
            "Um Esqueleto apareceu! Use o botão de movimento (▶) para avançar em direção a ele.",
        EtapaTutorial.AdicionarNaFila =>
            "Seus ataques apareceram na barra inferior! O número representa o dano que ele causa e as barras embaixo o tempo de recarga após o uso, Clique em um ataque para adicioná-lo à sua fila de ações.",
        EtapaTutorial.AproximarEsqueleto =>
            "O ataque foi adicionado a fila, agora dê mais um passo à direita (▶) para encostar no Esqueleto e deixá-lo no alcance do golpe.",
        EtapaTutorial.AtacarEsqueleto =>
            "Os botões de ação ficam à direita! Quando o tile do inimigo piscar em vermelho, ele vai atacar. Clique em (⚔) para executar sua fila de ações primeiro!",
        EtapaTutorial.ExplicarTurnos =>
            "Cada ação sua gasta 1 turno — como se mover, virar, adicionar ou remover um ataque da fila e executar os ataques. A cada turno os inimigos avançam suas ações e seus cooldowns diminuem!",
        EtapaTutorial.VirarParaArqueiro =>
            "Um Arqueiro surgiu nas suas costas! Seus ataques apontam para onde está olhando — clique no botão Virar (↻) para ficar de frente para ele.",
        EtapaTutorial.CaminharAteArqueiro when Turnos.Ctx?.Jogador.PosicaoPersonagem == 3 =>
            "O Arqueiro preparou a flecha! Dê mais um passo à esquerda (◀).",
        EtapaTutorial.CaminharAteArqueiro =>
            "Agora vá para a esquerda (◀) em direção ao Arqueiro e observe como ele reage antes de atirar.",
        EtapaTutorial.DerrotarArqueiro =>
            "Acertou! adicione um ataque à fila, aproxime-se (◀) e clique em Atacar (⚔) para derrotá-lo.",
        EtapaTutorial.MatarPrimeiroDosDois =>
            "Dois Esqueletos cercaram você! Adicione um ataque à fila e derrote primeiro o que está logo à sua esquerda.",
        EtapaTutorial.EsperarCooldown =>
            "Ótimo! Seu ataque entrou em cooldown (barrinhas escuras). Clique em Esperar (⏳) à direita para passar o turno e recuperar a recarga sem se mover!",
        EtapaTutorial.DerrotarUltimo =>
            "Muito bem! Esperar reduz o cooldown dos ataques em 1 turno. Agora use o que aprendeu para derrotar o último Esqueleto!",
        EtapaTutorial.DerrotarLanceiro => MensagemLanceiro(),
        _ => ""
    };

    private string MensagemLanceiro()
    {
        var fila = Turnos.Cartas.Where(c => c.EmFila).OrderBy(c => c.PosicaoFila).ToList();
        int idxMaldicao = fila.FindIndex(c => c.Ataque.Nome.Equals("Amaldiçoar", StringComparison.OrdinalIgnoreCase));

        if (fila.Count >= 2 && idxMaldicao > 0)
        {
            return "Use os botões ▲ / ▼ ao lado da fila para reordenar seus ataques sem gastar turno! Coloque Amaldiçoar embaixo (executa primeiro) para dobrar o dano da Espada, aproxime-se e clique em ⚔.";
        }
        if (fila.Count >= 2)
        {
            return "Ataques na fila podem ser reordenados a qualquer momento com ▲ / ▼ sem custo de turno! Agora encoste no Lanceiro e clique em Atacar (⚔) para finalizar o tutorial.";
        }
        return "Um Lanceiro apareceu na posição 4 e você recebeu Amaldiçoar! Ataques de borda vermelha têm Ação Rápida (não gastam turno na fila) e ataques com bolinha aplicam efeitos (Maldição dobra o próximo dano). Coloque Amaldiçoar e a Espada na fila e reordene com ▲ / ▼ sem gastar turno!";
    }

    private void AoMover(int dir)
    {
        if (!PodeMover(dir)) return;
        _ = Turnos.JogadorAgirAsync(new MoverAcao(dir));
    }

    private void AoVirar()
    {
        if (!PodeVirar()) return;
        _ = Turnos.JogadorAgirAsync(new VirarAcao());
    }

    private void AoEsperar()
    {
        if (!PodeEsperar()) return;
        _ = Turnos.JogadorAgirAsync(new EsperarAcao());
    }

    private void AoExecutarFila()
    {
        if (!PodeAtacar()) return;
        _ = Turnos.JogadorAgirAsync(new ExecutarFilaAcao(Turnos.Cartas));
    }

    private void LimparEstadoTutorial()
    {
        foreach (var c in Turnos.Cartas)
        {
            c.CooldownRestante = 0;
            c.PosicaoFila = null;
        }
        if (_PlayerService.CurrentCharacter != null)
        {
            _PlayerService.CurrentCharacter.ModoCombate = false;
            _PlayerService.CurrentCharacter.LimiteMapa = 5;
        }
    }

    private void IrParaLobby()
    {
        LimparEstadoTutorial();
        NavService.GoToLobby();
    }

    private void VoltarMenuInicial()
    {
        LimparEstadoTutorial();
        NavService.GoToHome();
    }

    public void Dispose()
    {
        if (_assinado)
        {
            Turnos.OnMudou -= AoMudar;
            Turnos.OnAcaoExecutada -= AoAcaoExecutada;
            Input.AoSolicitarMovimento -= AoMover;
            Input.AoSolicitarTrocaDirecao -= AoVirar;
            Input.AoSolicitarInteracao -= AoExecutarFila;
            Input.AoSolicitarEsperar -= AoEsperar;
        }
        LimparEstadoTutorial();
    }
}
