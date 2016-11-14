﻿using System;
using System.Collections.Generic;
using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using DFe.Utils.Assinatura;
using ManifestoDocumentoFiscalEletronico.Classes.Flags;
using ManifestoDocumentoFiscalEletronico.Classes.Informacoes;
using ManifestoDocumentoFiscalEletronico.Classes.Servicos.Flags;
using MDFe.AppTeste.Dao;
using MDFe.AppTeste.Entidades;
using MDFe.AppTeste.ModelBase;
using MDFe.Utils.Extencoes;
using Microsoft.Win32;

namespace MDFe.AppTeste
{
    public class MDFeTesteModel : ViewModel
    {
        private string _cnpj;
        private string _inscricaoEstadual;
        private string _nome;
        private string _nomeFantasia;
        private string _logradouro;
        private string _numero;
        private string _complemento;
        private string _bairro;
        private long _codigoIbgeMunicipio;
        private string _nomeMunicipio;
        private string _cep;
        private EstadoUF _siglaUf;
        private string _telefone;
        private string _email;
        private string _numeroDeSerie;
        private string _caminhoArquivo;
        private string _senha;
        private EstadoUF _ufDestino;
        private TipoAmbiente _ambiente;
        private short _serie;
        private long _numeracao;
        private VersaoServico _versaoMdFeRecepcao;
        private VersaoServico _versaoMdFeRetRecepcao;
        private VersaoServico _versaoMdFeRecepcaoEvento;
        private VersaoServico _versaoMdFeConsulta;
        private VersaoServico _versaoMdFeStatusServico;
        private VersaoServico _versaoMdFeConsNaoEnc;
        private bool _ambienteProducao;
        private bool _ambienteHomologacao;

        #region empresa

        public string Cnpj
        {
            get { return _cnpj; }
            set
            {
                _cnpj = value;
                OnPropertyChanged("Cnpj");
            }
        }

        public string InscricaoEstadual
        {
            get { return _inscricaoEstadual; }
            set
            {
                _inscricaoEstadual = value;
                OnPropertyChanged("InscricaoEstadual");
            }
        }

        public string Nome
        {
            get { return _nome; }
            set
            {
                _nome = value;
                OnPropertyChanged("Nome");
            }
        }

        public string NomeFantasia
        {
            get { return _nomeFantasia; }
            set
            {
                _nomeFantasia = value;
                OnPropertyChanged("NomeFantasia");
            }
        }

        public string Logradouro
        {
            get { return _logradouro; }
            set
            {
                _logradouro = value;
                OnPropertyChanged("Logradouro");
            }
        }

        public string Numero
        {
            get { return _numero; }
            set
            {
                _numero = value;
                OnPropertyChanged("Numero");
            }
        }

        public string Complemento
        {
            get { return _complemento; }
            set
            {
                _complemento = value;
                OnPropertyChanged("Complemento");
            }
        }

        public string Bairro
        {
            get { return _bairro; }
            set
            {
                _bairro = value;
                OnPropertyChanged("Bairro");
            }
        }

        public long CodigoIbgeMunicipio
        {
            get { return _codigoIbgeMunicipio; }
            set
            {
                _codigoIbgeMunicipio = value;
                OnPropertyChanged("CodigoIbgeMunicipio");
            }
        }

        public string NomeMunicipio
        {
            get { return _nomeMunicipio; }
            set
            {
                _nomeMunicipio = value;
                OnPropertyChanged("NomeMunicipio");
            }
        }

        public string Cep
        {
            get { return _cep; }
            set
            {
                _cep = value;
                OnPropertyChanged("Cep");
            }
        }

        public EstadoUF SiglaUf
        {
            get { return _siglaUf; }
            set
            {
                _siglaUf = value;
                OnPropertyChanged("SiglaUf");
            }
        }

        public string Telefone
        {
            get { return _telefone; }
            set
            {
                _telefone = value;
                OnPropertyChanged("Telefone");
            }
        }

        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;
                OnPropertyChanged("Email");
            }
        }

        #endregion

        #region certificado

        public string NumeroDeSerie
        {
            get { return _numeroDeSerie; }
            set
            {
                _numeroDeSerie = value;
                OnPropertyChanged("NumeroDeSerie");
            }
        }

        public string CaminhoArquivo
        {
            get { return _caminhoArquivo; }
            set
            {
                _caminhoArquivo = value;
                OnPropertyChanged("CaminhoArquivo");
            }
        }

        public string Senha
        {
            get { return _senha; }
            set
            {
                _senha = value;
                OnPropertyChanged("Senha");
            }
        }

        #endregion

        #region configWebService

        public EstadoUF UfDestino
        {
            get { return _ufDestino; }
            set
            {
                _ufDestino = value;
                OnPropertyChanged("UfDestino");
            }
        }

        public TipoAmbiente Ambiente
        {
            get { return _ambiente; }
            set
            {
                _ambiente = value;
                OnPropertyChanged("Ambiente");
            }
        }

        public short Serie
        {
            get { return _serie; }
            set
            {
                _serie = value;
                OnPropertyChanged("Serie");
            }
        }

        public long Numeracao
        {
            get { return _numeracao; }
            set
            {
                _numeracao = value;
                OnPropertyChanged("Numeracao");
            }
        }

        public VersaoServico VersaoMDFeRecepcao
        {
            get { return _versaoMdFeRecepcao; }
            set
            {
                _versaoMdFeRecepcao = value;
                OnPropertyChanged("VersaoMDFeRecepcao");
            }
        }

        public VersaoServico VersaoMDFeRetRecepcao
        {
            get { return _versaoMdFeRetRecepcao; }
            set
            {
                _versaoMdFeRetRecepcao = value;
                OnPropertyChanged("VersaoMDFeRetRecepcao");
            }
        }

        public VersaoServico VersaoMDFeRecepcaoEvento
        {
            get { return _versaoMdFeRecepcaoEvento; }
            set
            {
                _versaoMdFeRecepcaoEvento = value;
                OnPropertyChanged("VersaoMDFeRecepcaoEvento");
            }
        }

        public VersaoServico VersaoMDFeConsulta
        {
            get { return _versaoMdFeConsulta; }
            set
            {
                _versaoMdFeConsulta = value;
                OnPropertyChanged("VersaoMDFeConsulta");
            }
        }

        public VersaoServico VersaoMDFeStatusServico
        {
            get { return _versaoMdFeStatusServico; }
            set
            {
                _versaoMdFeStatusServico = value;
                OnPropertyChanged("VersaoMDFeStatusServico");
            }
        }

        public VersaoServico VersaoMDFeConsNaoEnc
        {
            get { return _versaoMdFeConsNaoEnc; }
            set
            {
                _versaoMdFeConsNaoEnc = value;
                OnPropertyChanged("VersaoMDFeConsNaoEnc");
            }
        }

        public bool AmbienteProducao
        {
            get { return _ambienteProducao; }
            set
            {
                _ambienteProducao = value;
                OnPropertyChanged("AmbienteProducao");
            }
        }

        public bool AmbienteHomologacao
        {
            get { return _ambienteHomologacao; }
            set
            {
                _ambienteHomologacao = value;
                OnPropertyChanged("AmbienteHomologacao");
            }
        }

        #endregion

        public void SalvarConfiguracoesXml()
        {
            var configuracaoDao = new ConfiguracaoDao();
            var configuracaoAppTeste = new Configuracao {
                Empresa =
                    {
                        Cnpj = Cnpj,
                        Bairro = Bairro,
                        Cep = Cep,
                        CodigoIbgeMunicipio = CodigoIbgeMunicipio,
                        Complemento = Complemento,
                        Email = Email,
                        InscricaoEstadual = InscricaoEstadual,
                        Logradouro = Logradouro,
                        Nome = Nome,
                        NomeFantasia = NomeFantasia,
                        NomeMunicipio = NomeMunicipio,
                        Numero = Numero,
                        SiglaUf = SiglaUf,
                        Telefone = Telefone
                    },
                CertificadoDigital =
                {
                    CaminhoArquivo = CaminhoArquivo,
                    NumeroDeSerie = NumeroDeSerie,
                    Senha = Senha
                },
                ConfigWebService =
                {
                    UfDestino = UfDestino,
                    Numeracao = Numeracao,
                    Serie = Serie,
                    VersaoMDFeConsNaoEnc = VersaoServico.Versao100,
                    VersaoMDFeConsulta = VersaoServico.Versao100,
                    VersaoMDFeRecepcao = VersaoServico.Versao100,
                    VersaoMDFeRecepcaoEvento = VersaoServico.Versao100,
                    VersaoMDFeRetRecepcao = VersaoServico.Versao100,
                    VersaoMDFeStatusServico = VersaoServico.Versao100
                }
            };

            if (AmbienteHomologacao)
                configuracaoAppTeste.ConfigWebService.Ambiente = TipoAmbiente.Homologacao;

            if (AmbienteProducao)
                configuracaoAppTeste.ConfigWebService.Ambiente = TipoAmbiente.Producao;


            configuracaoDao.SalvarConfiguracao(configuracaoAppTeste);
        }

        public void ObterSerialCertificado()
        {
            var numeroSerie = CertificadoDigital.ObterDoRepositorio();
            NumeroDeSerie = numeroSerie.SerialNumber;
        }

        public void ObterCertificadoArquivo()
        {
            var janelaArquivo = new OpenFileDialog
            {
                Filter = "Certificado digital(*.pfx)|*.pfx"
            };
            if (janelaArquivo.ShowDialog() == true)
            {
                CaminhoArquivo = janelaArquivo.FileName;
            }
        }

        public void CarregarConfiguracoes()
        {
            var dao = new ConfiguracaoDao();
            var config = dao.BuscarConfiguracao();

            if (config == null) return;

            Cnpj = config.Empresa.Cnpj;
            Bairro = config.Empresa.Bairro;
            Cep = config.Empresa.Cep;
            CodigoIbgeMunicipio = config.Empresa.CodigoIbgeMunicipio;
            Complemento = config.Empresa.Complemento;
            Email = config.Empresa.Email;
            InscricaoEstadual = config.Empresa.InscricaoEstadual;
            Logradouro = config.Empresa.Logradouro;
            Nome = config.Empresa.Nome;
            NomeFantasia = config.Empresa.NomeFantasia;
            NomeMunicipio = config.Empresa.NomeMunicipio;
            Numero = config.Empresa.Numero;
            SiglaUf = config.Empresa.SiglaUf;
            Telefone = config.Empresa.Telefone;

            Senha = config.CertificadoDigital.Senha;
            CaminhoArquivo = config.CertificadoDigital.CaminhoArquivo;
            NumeroDeSerie = config.CertificadoDigital.NumeroDeSerie;


            AmbienteProducao = true;

            if (config.ConfigWebService.Ambiente == TipoAmbiente.Homologacao)
                AmbienteHomologacao = true;

            Numeracao = config.ConfigWebService.Numeracao;
            Serie = config.ConfigWebService.Serie;

            UfDestino = config.ConfigWebService.UfDestino;

            VersaoMDFeConsNaoEnc = config.ConfigWebService.VersaoMDFeConsNaoEnc;
            VersaoMDFeConsulta = config.ConfigWebService.VersaoMDFeConsulta;
            VersaoMDFeRecepcao = config.ConfigWebService.VersaoMDFeRecepcao;
            VersaoMDFeRecepcaoEvento = config.ConfigWebService.VersaoMDFeRecepcaoEvento;
            VersaoMDFeRetRecepcao = config.ConfigWebService.VersaoMDFeRetRecepcao;
            VersaoMDFeStatusServico = config.ConfigWebService.VersaoMDFeStatusServico;

        }

        public void CriarEnviar100()
        {
            var config = new ConfiguracaoDao().BuscarConfiguracao();

            Utils.Configuracoes.Configuracao.SenhaCertificadoDigital = config.CertificadoDigital.Senha;
            Utils.Configuracoes.Configuracao.CaminhoCertificadoDigital = config.CertificadoDigital.CaminhoArquivo;
            Utils.Configuracoes.Configuracao.NumeroSerieCertificadoDigital = config.CertificadoDigital.NumeroDeSerie;

            var mdfe = new ManifestoDocumentoFiscalEletronico.Classes.Informacoes.MDFe();

            #region (ide)
            mdfe.InfMDFe.Ide.CUF = config.ConfigWebService.UfDestino;
            mdfe.InfMDFe.Ide.TpAmb = config.ConfigWebService.Ambiente;
            mdfe.InfMDFe.Ide.TpEmit = MDFeTipoEmitente.PrestadorServicoDeTransporte;
            mdfe.InfMDFe.Ide.Mod = MDFeModelo.MDFe;
            mdfe.InfMDFe.Ide.Serie = 750;
            mdfe.InfMDFe.Ide.NMDF = ++config.ConfigWebService.Numeracao;
            mdfe.InfMDFe.Ide.CMDF = config.ConfigWebService.Numeracao;
            mdfe.InfMDFe.Ide.Modal = MDFeModal.Rodoviario;
            mdfe.InfMDFe.Ide.DhEmi = DateTime.Now;
            mdfe.InfMDFe.Ide.TpEmis = MDFeTipoEmissao.Normal;
            mdfe.InfMDFe.Ide.ProcEmi = MDFeIdentificacaoProcessoEmissao.EmissaoComAplicativoContribuinte;
            mdfe.InfMDFe.Ide.VerProc = "versao28383";
            mdfe.InfMDFe.Ide.UFIni = EstadoUF.GO;
            mdfe.InfMDFe.Ide.UFFim = EstadoUF.MT;


            mdfe.InfMDFe.Ide.InfMunCarrega.Add(new MDFeInfMunCarrega
            {
                CMunCarrega = "5211701",
                XMunCarrega = "JANDAIA"
            });

            mdfe.InfMDFe.Ide.InfMunCarrega.Add(new MDFeInfMunCarrega
            {
                CMunCarrega = "5209952",
                XMunCarrega = "INDIARA"
            });

            mdfe.InfMDFe.Ide.InfMunCarrega.Add(new MDFeInfMunCarrega
            {
                CMunCarrega = "5200134",
                XMunCarrega = "ACREUNA"
            });

            #endregion (ide)

            #region dados emitente (emit)
            mdfe.InfMDFe.Emit.CNPJ = config.Empresa.Cnpj;
            mdfe.InfMDFe.Emit.IE = config.Empresa.InscricaoEstadual;
            mdfe.InfMDFe.Emit.XNome = config.Empresa.Nome;
            mdfe.InfMDFe.Emit.XFant = config.Empresa.NomeFantasia;

            mdfe.InfMDFe.Emit.EnderEmit.XLgr = config.Empresa.Logradouro;
            mdfe.InfMDFe.Emit.EnderEmit.Nro = config.Empresa.Numero;
            mdfe.InfMDFe.Emit.EnderEmit.XCpl = config.Empresa.Complemento;
            mdfe.InfMDFe.Emit.EnderEmit.XBairro = config.Empresa.Bairro;
            mdfe.InfMDFe.Emit.EnderEmit.CMun = config.Empresa.CodigoIbgeMunicipio;
            mdfe.InfMDFe.Emit.EnderEmit.XMun = config.Empresa.NomeMunicipio;
            mdfe.InfMDFe.Emit.EnderEmit.CEP = long.Parse(config.Empresa.Cep);
            mdfe.InfMDFe.Emit.EnderEmit.UF = config.Empresa.SiglaUf;
            mdfe.InfMDFe.Emit.EnderEmit.Fone = config.Empresa.Telefone;
            mdfe.InfMDFe.Emit.EnderEmit.Email = config.Empresa.Email;
            #endregion dados emitente (emit)

            #region modal
            mdfe.InfMDFe.InfModal.Modal = new MDFeRodo
            {
                RNTRC = config.Empresa.RNTRC,
                VeicTracao = new MDFeVeicTracao
                {
                    Placa = "kkk888",
                    RENAVAM = "888888888",
                    UF = EstadoUF.GO,
                    Tara = 222,
                    CapM3 = 222,
                    CapKG = 22,
                    Condutor = new MDFeCondutor
                    {
                        CPF = "11392381754",
                        XNome = "Ricardão"
                    },
                    TpRod = MDFeTpRod.Outros,
                    TpCar = MDFeTpCar.NaoAplicavel
                }
            };
            #endregion modal




            mdfe = mdfe.Assina();

            var xmlEnvio = FuncoesXml.ClasseParaXmlString(mdfe);

            var xm = xmlEnvio;

            /*


            mdfe.InfMDFe.InfDoc.InfMunDescarga = new List<MDFeInfMunDescarga>
            {
                new MDFeInfMunDescarga
                {
                    CMunDescarga = "CUIABA",
                    XMunDescarga = "5103403",
                    InfCTe = new List<MDFeInfCTe>
                    {
                        new MDFeInfCTe
                        {
                            ChCTe = "52161021351378000100577500000000191194518006"
                        }
                    }
                }
            };

            mdfe.InfMDFe.Tot.QCTe = 1;
            mdfe.InfMDFe.Tot.vCarga = 500.00m;
            mdfe.InfMDFe.Tot.CUnid = MDFeCUnid.KG;
            mdfe.InfMDFe.Tot.QCarga = 100.0000m;

            mdfe.InfMDFe.InfAdic = new MDFeInfAdic
            {
                InfCpl = "aaaaaaaaaaaaaaaa"
            };*/
        }
    }
}