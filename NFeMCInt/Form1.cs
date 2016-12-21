using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web.Services.Protocols;
using System.Windows;
using System.Windows.Forms;
using BLL;
using Model;
using NFe;
using NFe.Classes;
using NFe.Classes.Informacoes;
using NFe.Classes.Informacoes.Cobranca;
using NFe.Classes.Informacoes.Destinatario;
using NFe.Classes.Informacoes.Detalhe;
using NFe.Classes.Informacoes.Detalhe.Tributacao;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual.Tipos;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Federal;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Federal.Tipos;
using NFe.Classes.Informacoes.Emitente;
using NFe.Classes.Informacoes.Identificacao;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Classes.Informacoes.Observacoes;
using NFe.Classes.Informacoes.Pagamento;
using NFe.Classes.Informacoes.Total;
using NFe.Classes.Informacoes.Transporte;
using NFe.Classes.Servicos.ConsultaCadastro;
using NFe.Classes.Servicos.Tipos;
using NFe.Servicos;
using NFe.Servicos.Retorno;
using NFe.Utils;
using NFe.Utils.Assinatura;
using NFe.Utils.Email;
using NFe.Utils.InformacoesSuplementares;
using NFe.Utils.NFe;
using NFe.Utils.Tributacao.Estadual;
using System.Data;
using NFe.Danfe.Fast.NFe;
using NFe.Classes.Informacoes.Detalhe.DeclaracaoImportacao;
using NFe.Classes.Protocolo;
using System.Diagnostics;
//using RichTextBox = System.Windows.Controls.RichTextBox;
//using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
//using WebBrowser = System.Windows.Controls.WebBrowser;

namespace NFeMCInt
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CarregarConfiguracao();
            //DataContext = _configuracoes;
        }
        private const string ArquivoConfiguracao = @"\configuracao.xml";
        private const string TituloErro = "Erro";
        private ConfiguracaoApp _configuracoes;
        private NFe.Classes.NFe _nfe;
        private readonly string _path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private DataSet _nfa;
        private string _lastXmot;
        private string _lastCstat;

        private void Form1_Load(object sender, EventArgs e)
        {
            new SessionBLL().Connect(new SessionModel() { Server = "10.0.0.199", Database = "nova", User = "root", Password = "beleza" });

            ClienteBLL cBll = new ClienteBLL();
            ClientesModel c = cBll.FrameworkGetOneModel(1000);
            pictureBox1.Image = imageList1.Images[Convert.ToInt32(!timer1.Enabled)];


        }
        private void SalvarConfiguracao()
        {
            try
            {
                _configuracoes.SalvarParaAqruivo(_path + ArquivoConfiguracao);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                    MessageBox.Show(string.Format("{0} \n\nDetalhes: {1}", ex.Message, ex.InnerException), "Erro",
                        MessageBoxButtons.OK);
            }
        }
        private void CarregarConfiguracao()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            try
            {
                _configuracoes = !File.Exists(path + ArquivoConfiguracao)
                    ? new ConfiguracaoApp()
                    : FuncoesXml.ArquivoXmlParaClasse<ConfiguracaoApp>(path + ArquivoConfiguracao);
                if (_configuracoes.CfgServico.TimeOut == 0)
                    _configuracoes.CfgServico.TimeOut = 3000; //mínimo
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                    MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            EnviaNFe(textBox1.Text);
        }

        public void EnviaNFe(string nfe)
        {
            try
            {
                #region Cria e Envia NFe

                var numero = nfe;
                var lote = "6487";

                _nfa = new NFeBLL().GetNfaDataTable(numero);

                _nfe = GetNf(Convert.ToInt32(numero), _configuracoes.CfgServico.ModeloDocumento,
                    _configuracoes.CfgServico.VersaoNFeAutorizacao);
                //_nfe.SalvarArquivoXml(_configuracoes.CfgServico.DiretorioSalvarXml + "\\Teste.xml");
                _nfe.Assina(); //não precisa validar aqui, pois o lote será validado em ServicosNFe.NFeAutorizacao
                //A URL do QR-Code deve ser gerada em um objeto nfe já assinado, pois na URL vai o DigestValue que é gerado por ocasião da assinatura
                //_nfe.infNFeSupl = new infNFeSupl() { qrCode = _nfe.infNFeSupl.ObterUrlQrCode(_nfe, _configuracoes.ConfiguracaoCsc.CIdToken, _configuracoes.ConfiguracaoCsc.Csc) }; //Define a URL do QR-Code.
                var servicoNFe = new ServicosNFe(_configuracoes.CfgServico);


                richTextBox1.Clear();

                //Assincrono
                //var retornoEnvio = servicoNFe.NFeAutorizacao(Convert.ToInt32(lote), IndicadorSincronizacao.Assincrono, new List<NFe.Classes.NFe> { _nfe }, true/*Envia a mensagem compactada para a SEFAZ*/);
                //Para consumir o serviço de forma síncrona, use a linha abaixo:
                var retornoEnvio = servicoNFe.NFeAutorizacao(Convert.ToInt32(lote), IndicadorSincronizacao.Sincrono, new List<NFe.Classes.NFe> { _nfe }, true/*Envia a mensagem compactada para a SEFAZ*/);

                TrataRetorno(retornoEnvio);

                System.Threading.Thread.Sleep(3000);

                var retornoRecibo = servicoNFe.NFeRetAutorizacao(retornoEnvio.Retorno.infRec.nRec);
                TrataRetorno(retornoRecibo);
                richTextBox1.Text = retornoRecibo.RetornoCompletoStr;
                textBox4.Text = retornoRecibo.Retorno.protNFe[0].infProt.cStat.ToString();
                textBox5.Text = retornoRecibo.Retorno.protNFe[0].infProt.xMotivo;

                if (retornoRecibo.Retorno.protNFe[0].infProt.cStat == 100)
                {

                    var nfeproc = new nfeProc
                    {
                        NFe = _nfe,
                        protNFe = retornoRecibo.Retorno.protNFe[0],
                        versao = retornoRecibo.Retorno.versao
                    };
                    if (nfeproc.protNFe != null)
                    {
                        var novoArquivo = Path.GetDirectoryName(_configuracoes.CfgServico.DiretorioSalvarXml) + @"\" + nfeproc.protNFe.infProt.chNFe +
                                          "-procNfe.xml";
                        FuncoesXml.ClasseParaArquivoXml(nfeproc, novoArquivo);
                    }


                    Imprimir(nfeproc.ObterXmlString());
                    //var retornoDownload = servicoNFe.NfeDownloadNf("64877996000109", 
                    //    new List<string>() { _nfa.Tables["nfe_cab"].Rows[0]["chnfe"].ToString() });

                    ////Se desejar consultar mais de uma chave, use o serviço como indicado abaixo. É permitido consultar até 10 nfes de uma vez.
                    ////Leia atentamente as informações do consumo deste serviço constantes no manual
                    ////var retornoDownload = servicoNFe.NfeDownloadNf(cnpj, new List<string>() { "28150707703290000189550010000009441000029953", "28150707703290000189550010000009431000029948" });

                    //TrataRetorno(retornoDownload);
                }
                else
                {
                    RetornoXml(webBrowser1, _nfe.ObterXmlString());
                }



                #endregion
            }
            catch (Exception ex)
            {

                richTextBox1.Text = ex.Message;
                if (ex.InnerException != null)
                    richTextBox1.Text += ex.InnerException.Message;

                if (ex is SoapException | ex is InvalidOperationException | ex is WebException)
                {
                    //Faça o tratamento de contingência OffLine aqui. Em produção, acredito que tratar apenas as exceções SoapException e WebException sejam suficientes
                    //Ver https://msdn.microsoft.com/pt-br/library/system.web.services.protocols.soaphttpclientprotocol.invoke(v=vs.110).aspx
                    //throw;
                }
                //if (!string.IsNullOrEmpty(ex.Message))
                //    Funcoes.Mensagem(ex.Message, "Erro", MessageBoxButton.OK);
            }
        }

        public protNFe EnviaNFeSilent(string nfe, out string xml)
        {
            xml = "";
            try
            {
                #region Cria e Envia NFe

                var numero = nfe;
                var lote = "6487";

                _nfa = new NFeBLL().GetNfaDataTable(numero);

                _nfe = GetNf(Convert.ToInt32(numero), _configuracoes.CfgServico.ModeloDocumento,
                    _configuracoes.CfgServico.VersaoNFeAutorizacao);
                //_nfe.SalvarArquivoXml(_configuracoes.CfgServico.DiretorioSalvarXml + "\\Teste.xml");
                _nfe.Assina(); //não precisa validar aqui, pois o lote será validado em ServicosNFe.NFeAutorizacao
                //A URL do QR-Code deve ser gerada em um objeto nfe já assinado, pois na URL vai o DigestValue que é gerado por ocasião da assinatura
                //_nfe.infNFeSupl = new infNFeSupl() { qrCode = _nfe.infNFeSupl.ObterUrlQrCode(_nfe, _configuracoes.ConfiguracaoCsc.CIdToken, _configuracoes.ConfiguracaoCsc.Csc) }; //Define a URL do QR-Code.
                var servicoNFe = new ServicosNFe(_configuracoes.CfgServico);

                //Assincrono
                //var retornoEnvio = servicoNFe.NFeAutorizacao(Convert.ToInt32(lote), IndicadorSincronizacao.Assincrono, new List<NFe.Classes.NFe> { _nfe }, true/*Envia a mensagem compactada para a SEFAZ*/);
                //Para consumir o serviço de forma síncrona, use a linha abaixo:
                var retornoEnvio = servicoNFe.NFeAutorizacao(Convert.ToInt32(lote), IndicadorSincronizacao.Sincrono, new List<NFe.Classes.NFe> { _nfe }, true/*Envia a mensagem compactada para a SEFAZ*/);

                System.Threading.Thread.Sleep(3000);

                var retornoRecibo = servicoNFe.NFeRetAutorizacao(retornoEnvio.Retorno.infRec.nRec);

                if (retornoRecibo.Retorno.protNFe[0].infProt.cStat == 100) 
                {

                    var nfeproc = new nfeProc
                    {
                        NFe = _nfe,
                        protNFe = retornoRecibo.Retorno.protNFe[0],
                        versao = retornoRecibo.Retorno.versao
                    };
                    if (nfeproc.protNFe != null)
                    {
                        var novoArquivo = Path.GetDirectoryName(_configuracoes.CfgServico.DiretorioSalvarXml) + @"\" + nfeproc.protNFe.infProt.chNFe +
                                          "-procNfe.xml";
                        FuncoesXml.ClasseParaArquivoXml(nfeproc, novoArquivo);
                        xml = FuncoesXml.ClasseParaXmlString(nfeproc);
                    }

                }
                else if(retornoRecibo.Retorno.protNFe[0].infProt.cStat == 204)
                {
                    var retornoConsulta = servicoNFe.NfeConsultaProtocolo(_nfa.Tables["nfe_cab"].Rows[0]["chnfe"].ToString());
                    var nfeproc = new nfeProc
                    {
                        NFe = _nfe,
                        protNFe = retornoConsulta.Retorno.protNFe,
                        versao = retornoConsulta.Retorno.versao
                    };
                    if (nfeproc.protNFe != null)
                    {
                        var novoArquivo = Path.GetDirectoryName(_configuracoes.CfgServico.DiretorioSalvarXml) + @"\" + nfeproc.protNFe.infProt.chNFe +
                                          "-procNfe.xml";
                        FuncoesXml.ClasseParaArquivoXml(nfeproc, novoArquivo);
                        xml = FuncoesXml.ClasseParaXmlString(nfeproc);
                    }

                    return retornoConsulta.Retorno.protNFe;
                }

                return retornoRecibo.Retorno.protNFe[0];

                #endregion
            }
            catch (Exception ex)
            {
                _lastCstat = "8";
                _lastXmot = ex.Message;
                richTextBox1.Text += ex.Message;
                if (ex is SoapException | ex is InvalidOperationException | ex is WebException)
                {
                    //throw;
                }
                return null;
            }
        }

        protected virtual NFe.Classes.NFe GetNf(int numero, ModeloDocumento modelo, VersaoServico versao)
        {
            var nf = new NFe.Classes.NFe { infNFe = GetInf(numero, modelo, versao) };
            return nf;
        }

        protected virtual infNFe GetInf(int numero, ModeloDocumento modelo, VersaoServico versao)
        {
            var infNFe = new infNFe
            {
                versao = versao.VersaoServicoParaString(),
                ide = GetIdentificacao(numero, modelo, versao),
                emit = GetEmitente(),
                dest = GetDestinatario(versao, modelo),
                transp = GetTransporte()
            };

            for (int i = 0; i < _nfa.Tables["nfe_item"].Rows.Count; i++)
            {
                infNFe.det.Add(GetDetalhe(i, infNFe.emit.CRT, modelo));
            }

            infNFe.total = GetTotal(versao, infNFe.det);

            if (infNFe.ide.mod == ModeloDocumento.NFe & versao == VersaoServico.ve310)
                infNFe.cobr = GetCobranca(infNFe.total.ICMSTot); //V3.00 Somente
            if (infNFe.ide.mod == ModeloDocumento.NFCe)
                infNFe.pag = GetPagamento(infNFe.total.ICMSTot); //NFCe Somente  

            //if (infNFe.ide.mod == ModeloDocumento.NFCe)
            infNFe.infAdic = new infAdic() { infCpl = _nfa.Tables["nfe_cab"].Rows[0]["infcpl"].ToString().RemoveAccents().ToUpper().Replace("\n",""), };



            if (infNFe.dest.enderDest.UF == "EX")
            {
                infNFe.exporta = new exporta()
                {
                    UFSaidaPais = _nfa.Tables["nfe_cab"].Rows[0]["exporta_ufembarq"].ToString(),
                    xLocDespacho = _nfa.Tables["nfe_cab"].Rows[0]["exporta_xlocembarq"].ToString(),
                    xLocExporta = _nfa.Tables["nfe_cab"].Rows[0]["exporta_xlocembarq"].ToString(),
                };
            }

            return infNFe;
        }

        protected virtual ide GetIdentificacao(int numero, ModeloDocumento modelo, VersaoServico versao)
        {
            var ide = new ide
            {
                cUF = Estado.SP,
                natOp = _nfa.Tables["nfe_cab"].Rows[0]["ide_natop"].ToString(),
                indPag = IndicadorPagamento.ipVista,
                mod = modelo,
                serie = 1,
                nNF = numero,
                tpNF = Convert.ToInt32(_nfa.Tables["nfe_cab"].Rows[0]["ide_tpnf"].ToString()) == 1 ? TipoNFe.tnSaida : TipoNFe.tnEntrada,
                cMunFG = Convert.ToInt64(_nfa.Tables["nfe_emitente"].Rows[0]["cmun"].ToString()),
                tpEmis = _configuracoes.CfgServico.tpEmis,
                tpImp = TipoImpressao.tiRetrato,
                cNF = _nfa.Tables["nfe_cab"].Rows[0]["ide_cnf"].ToString(),
                tpAmb = _configuracoes.CfgServico.tpAmb,
                finNFe = (FinalidadeNFe)Convert.ToInt32(_nfa.Tables["nfe_cab"].Rows[0]["ide_finnfe"].ToString()),
                verProc = "3.000",
                idDest = (DestinoOperacao)Convert.ToInt32(_nfa.Tables["nfe_cab"].Rows[0]["ide_iddest"].ToString()),

            };

            if (_nfa.Tables["nfe_ref"].Rows.Count > 0)
            {
                ide.NFref = new List<NFref>();

                foreach (DataRow dr in _nfa.Tables["nfe_ref"].Rows)
                {
                    ide.NFref.Add(new NFref()
                    {
                        refNFe = dr["nfe"].ToString(),
                        //refNF = new refNF()
                        //{
                        //    AAMM = dr["aamm"].ToString(),
                        //    cUF = Estado.SP,
                        //    CNPJ = dr["cnpj"].ToString(),
                        //    mod = dr["mod_"].ToString(),
                        //    //serie = Convert.ToInt32(dr["serie"].ToString()),
                        //    //nNF = Convert.ToInt32(dr["nnf"].ToString()),
                        //}
                    });
                }


            }

            if (ide.tpEmis != TipoEmissao.teNormal)
            {
                ide.dhCont =
                    DateTime.Now.ToString(versao == VersaoServico.ve310
                        ? "yyyy-MM-ddTHH:mm:sszzz"
                        : "yyyy-MM-ddTHH:mm:ss");
                ide.xJust = "TESTE DE CONTIGÊNCIA PARA NFe/NFCe";
            }

            #region V2.00

            if (versao == VersaoServico.ve200)
            {
                ide.dEmi = DateTime.Today.ToString("yyyy-MM-dd"); //Mude aqui para enviar a nfe vinculada ao EPEC, V2.00
                ide.dSaiEnt = DateTime.Today.ToString("yyyy-MM-dd");
            }

            #endregion

            #region V3.00

            if (versao != VersaoServico.ve310) return ide;
            ide.dhEmi = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            //Mude aqui para enviar a nfe vinculada ao EPEC, V3.10
            if (ide.mod == ModeloDocumento.NFe)
                ide.dhSaiEnt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            else
                ide.tpImp = TipoImpressao.tiNFCe;
            ide.procEmi = ProcessoEmissao.peAplicativoContribuinte;
            ide.indFinal = ConsumidorFinal.cfNao; //NFCe: Tem que ser consumidor Final
            ide.indPres = PresencaComprador.pcNao; //NFCe: deve ser 1 ou 4


            #endregion

            return ide;
        }

        protected virtual emit GetEmitente()
        {
            var emit = new emit//_configuracoes.Emitente; // new emit
            {
                //CPF = "80365027553",
                CNPJ = _nfa.Tables["nfe_emitente"].Rows[0]["cnpj"].ToString(),
                xNome = _nfa.Tables["nfe_emitente"].Rows[0]["nome"].ToString(),
                xFant = _nfa.Tables["nfe_emitente"].Rows[0]["fant"].ToString(),
                IE = _nfa.Tables["nfe_emitente"].Rows[0]["ie"].ToString(),
                IM = _nfa.Tables["nfe_emitente"].Rows[0]["im"].ToString(),
                CRT = CRT.RegimeNormal,
                CNAE = _nfa.Tables["nfe_emitente"].Rows[0]["cnae"].ToString(),
            };
            emit.enderEmit = GetEnderecoEmitente();

            if (!string.IsNullOrEmpty(_nfa.Tables["nfe_cab"].Rows[0]["iest"].ToString()))
                emit.IEST = _nfa.Tables["nfe_cab"].Rows[0]["iest"].ToString();

            return emit;
        }

        protected virtual enderEmit GetEnderecoEmitente()
        {
            var enderEmit = new enderEmit //_configuracoes.EnderecoEmitente; // new enderEmit
            {
                xLgr = _nfa.Tables["nfe_emitente"].Rows[0]["ender"].ToString(),
                nro = _nfa.Tables["nfe_emitente"].Rows[0]["nro"].ToString(),
                xCpl = _nfa.Tables["nfe_emitente"].Rows[0]["cpl"].ToString(),
                xBairro = _nfa.Tables["nfe_emitente"].Rows[0]["bairro"].ToString(),
                cMun = Convert.ToInt64(_nfa.Tables["nfe_emitente"].Rows[0]["cmun"].ToString()),
                xMun = _nfa.Tables["nfe_emitente"].Rows[0]["cmun"].ToString(),
                UF = _nfa.Tables["nfe_emitente"].Rows[0]["uf"].ToString(),
                CEP = _nfa.Tables["nfe_emitente"].Rows[0]["cep"].ToString(),
                fone = Convert.ToInt64(_nfa.Tables["nfe_emitente"].Rows[0]["fone"].ToString()),

            };
            enderEmit.cPais = Convert.ToInt32(_nfa.Tables["nfe_emitente"].Rows[0]["cpais"].ToString());
            enderEmit.xPais = _nfa.Tables["nfe_emitente"].Rows[0]["pais"].ToString();


            

            return enderEmit;
        }

        protected virtual dest GetDestinatario(VersaoServico versao, ModeloDocumento modelo)
        {
            var dest = new dest(versao)
            {
                CNPJ = _nfa.Tables["nfe_cab"].Rows[0]["dest_cnpj"].ToString(),
                CPF = _nfa.Tables["nfe_cab"].Rows[0]["dest_cpf"].ToString(),
            };
            if (modelo == ModeloDocumento.NFe)
            {
                dest.xNome = _nfa.Tables["nfe_cab"].Rows[0]["dest_xnome"].ToString(); //Obrigatório para NFe e opcional para NFCe
                dest.enderDest = GetEnderecoDestinatario(); //Obrigatório para NFe e opcional para NFCe
                dest.IE = _nfa.Tables["nfe_cab"].Rows[0]["dest_ie"].ToString();

                if (!string.IsNullOrEmpty(_nfa.Tables["nfe_cab"].Rows[0]["idestrangeiro"].ToString()))
                {
                    dest.idEstrangeiro = _nfa.Tables["nfe_cab"].Rows[0]["idestrangeiro"].ToString();
                    dest.enderDest.CEP = "99999999";
                    dest.enderDest.xMun = "EXTERIOR";
                    dest.enderDest.UF = "EX";

                }



                //Verificando se está no ambiente de Homologação
                if (_configuracoes.CfgServico.tpAmb == TipoAmbiente.taHomologacao)
                {
                    dest.xNome = "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL";
                    //dest.IE = "";
                    //dest.CNPJ = "99999999000191";
                }

            }

            //if (versao == VersaoServico.ve200)
            //    dest.IE = "ISENTO";
            if (versao != VersaoServico.ve310) return dest;
            dest.indIEDest = (indIEDest)Convert.ToInt32(_nfa.Tables["nfe_cab"].Rows[0]["indiedest"].ToString()); //NFCe: Tem que ser não contribuinte V3.00 Somente
            dest.email = _nfa.Tables["nfe_cab"].Rows[0]["dest_email"].ToString(); //V3.00 Somente
            return dest;
        }

        protected virtual enderDest GetEnderecoDestinatario()
        {

            if (_nfa.Tables["nfe_cab"].Rows[0]["enderdest_cmun"].ToString() == "" &&
                _nfa.Tables["nfe_cab"].Rows[0]["enderdest_cpais"].ToString() != "1058")
            {
                _nfa.Tables["nfe_cab"].Rows[0]["enderdest_cmun"] = "9999999";
            }

            var enderDest = new enderDest
            {
                xLgr = _nfa.Tables["nfe_cab"].Rows[0]["enderdest_xlgr"].ToString(),
                nro = _nfa.Tables["nfe_cab"].Rows[0]["enderdest_nro"].ToString(),
                xCpl = _nfa.Tables["nfe_cab"].Rows[0]["enderdest_xcpl"].ToString(),
                xBairro = _nfa.Tables["nfe_cab"].Rows[0]["enderdest_xbairro"].ToString(),
                cMun = Convert.ToInt64(_nfa.Tables["nfe_cab"].Rows[0]["enderdest_cmun"].ToString()),
                xMun = _nfa.Tables["nfe_cab"].Rows[0]["enderdest_xmun"].ToString(),
                UF = _nfa.Tables["nfe_cab"].Rows[0]["enderdest_uf"].ToString(),
                CEP = _nfa.Tables["nfe_cab"].Rows[0]["enderdest_cep"].ToString(),
                cPais = Convert.ToInt32(_nfa.Tables["nfe_cab"].Rows[0]["enderdest_cpais"].ToString()),
                xPais = _nfa.Tables["nfe_cab"].Rows[0]["enderdest_xpais"].ToString(),
                //fone = Convert.ToInt64(_nfa.Tables["nfe_cab"].Rows[0]["enderdest_fone"].ToString()),

            };

            if (enderDest.xPais == "" && enderDest.cPais == 1058)
                enderDest.xPais = "Brasil";

            return enderDest;
        }

        protected virtual det GetDetalhe(int i, CRT crt, ModeloDocumento modelo)
        {
            ICMSGeral ic = new NFe.Utils.Tributacao.Estadual.ICMSGeral();
            var detalhe = new det();
            List<DI> listaDI = null;
            II imp_import = null;

            //Importacao
            if (!string.IsNullOrEmpty(_nfa.Tables["nfe_item"].Rows[i]["di_ndi"].ToString()))
            {
                List<adi> lista_adi = new List<adi>();

                foreach (DataRow item in _nfa.Tables["nfe_itemadi"].Rows)
                {
                    if (item["item"].ToString() == _nfa.Tables["nfe_item"].Rows[i]["item"].ToString())
                    {
                        adi item_adi = new adi();

                        item_adi.nAdicao = Convert.ToInt32(item["adi_nadicao"].ToString());
                        item_adi.nSeqAdic = Convert.ToInt32(item["adi_nseqadic"].ToString());
                        //item_adi.vDescDI = Convert.ToDecimal(item["adi_vdescdi"].ToString());
                        item_adi.cFabricante = item["adi_cfabricante"].ToString();

                        lista_adi.Add(item_adi);
                    }
                }


                DI di_item = new DI()
                {
                    nDI = _nfa.Tables["nfe_item"].Rows[i]["di_ndi"].ToString(),
                    dDI = Convert.ToDateTime(_nfa.Tables["nfe_item"].Rows[i]["di_ddi"].ToString()).ToString("yyyy-MM-dd"),
                    xLocDesemb = _nfa.Tables["nfe_item"].Rows[i]["di_xlocdesemb"].ToString(),
                    dDesemb = Convert.ToDateTime(_nfa.Tables["nfe_item"].Rows[i]["di_ddesemb"].ToString()).ToString("yyyy-MM-dd"),
                    UFDesemb = _nfa.Tables["nfe_item"].Rows[i]["di_ufdesemb"].ToString(),
                    cExportador = _nfa.Tables["nfe_item"].Rows[i]["di_cexportador"].ToString(),
                    tpViaTransp = TipoTransporteInternacional.MeiosProprios,
                    tpIntermedio = TipoIntermediacao.ContaPropria,
                    adi = lista_adi

                };



                imp_import = new II()
                {
                    vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ii_vbc"].ToString()),
                    vII = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ii_vii"].ToString()),
                    vIOF = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ii_viof"].ToString()),
                    vDespAdu = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ii_vdespadu"].ToString()),
                };

                listaDI = new List<DI>();
                listaDI.Add(di_item);
            }


            detalhe.nItem = i + 1;
            detalhe.prod = new prod()
            {
                cProd = _nfa.Tables["nfe_item"].Rows[i]["prod_cprod"].ToString(),
                cEAN = _nfa.Tables["nfe_item"].Rows[i]["prod_cean"].ToString(),
                xProd = _nfa.Tables["nfe_item"].Rows[i]["prod_xprod"].ToString(),
                NCM = _nfa.Tables["nfe_item"].Rows[i]["prod_ncm"].ToString(),
                //EXTIPI = _nfa.Tables["nfe_item"].Rows[i]["prod_extipi"].ToString(),
                CFOP = Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["prod_cfop"].ToString()),
                uCom = _nfa.Tables["nfe_item"].Rows[i]["prod_ucom"].ToString(),
                qCom = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["prod_qcom"].ToString()),
                vUnCom = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["prod_vuncom"].ToString()),
                vProd = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["prod_vprod"].ToString()),
                cEANTrib = _nfa.Tables["nfe_item"].Rows[i]["prod_ceantrib"].ToString(),
                uTrib = _nfa.Tables["nfe_item"].Rows[i]["prod_utrib"].ToString(),
                qTrib = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["prod_qtrib"].ToString()),
                vUnTrib = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["prod_vuntrib"].ToString()),
                vFrete = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["prod_vfrete"].ToString()),
                vSeg = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["prod_vseg"].ToString()),
                vDesc = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["prod_vdesc"].ToString()),
                vOutro = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["prod_voutro"].ToString()),
                indTot = IndicadorTotal.ValorDoItemCompoeTotalNF,
                DI = listaDI,


                //CEST = _nfa.Tables["nfe_item"].Rows[i]["prod_cest"].ToString(),
                //xPed = _nfa.Tables["nfe_item"].Rows[i]["xped"].ToString(),
                //nItemPed = Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["nitemped"].ToString()),

            };

            if (!string.IsNullOrEmpty(_nfa.Tables["nfe_item"].Rows[i]["prod_cest"].ToString()))
            {
                detalhe.prod.CEST = _nfa.Tables["nfe_item"].Rows[i]["prod_cest"].ToString();
            }

            string CSTstring = _nfa.Tables["nfe_item"].Rows[i]["icms_cst"].ToString();
            string modBCString = _nfa.Tables["nfe_item"].Rows[i]["icms_modbc"].ToString();
            string modBCSSTtring = _nfa.Tables["nfe_item"].Rows[i]["icms_modbcst"].ToString();
            string origString = _nfa.Tables["nfe_item"].Rows[i]["icms_orig"].ToString();
            string ipiString = _nfa.Tables["nfe_item"].Rows[i]["ipitrib_cst"].ToString();

            DeterminacaoBaseIcms detBC = (DeterminacaoBaseIcms)int.Parse(modBCString);
            DeterminacaoBaseIcmsSt detBCST = (DeterminacaoBaseIcmsSt)int.Parse(modBCSSTtring);
            OrigemMercadoria origem = (OrigemMercadoria)int.Parse(origString);
            Csticms cs = new Csticms();

            switch (CSTstring)
            {
                case "00":
                    cs = Csticms.Cst00;
                    break;
                case "10":
                    cs = Csticms.Cst10;
                    break;
                case "30":
                    cs = Csticms.Cst30;
                    break;
                case "40":
                    cs = Csticms.Cst40;
                    break;
                case "41":
                    cs = Csticms.Cst41;
                    break;
                case "50":
                    cs = Csticms.Cst50;
                    break;
                case "90":
                    cs = Csticms.Cst90;
                    break;
                default:
                    break;
            }

            ICMSBasico icms = new ICMS10();

            switch (cs)
            {
                case Csticms.Cst00:
                    icms = new ICMS00()
                    {
                        CST = cs,
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_vbc"].ToString()),
                        pICMS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_picms"].ToString()),
                        vICMS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_vicms"].ToString()),
                        modBC = detBC,
                        orig = origem
                    };

                    break;
                case Csticms.Cst10:
                    icms = new ICMS10()
                    {
                        CST = cs,
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_vbc"].ToString()),
                        pICMS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_picms"].ToString()),
                        vICMS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_vicms"].ToString()),
                        pMVAST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_pmvast"].ToString()),
                        pRedBCST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_predbcst"].ToString()),
                        vBCST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_vbcst"].ToString()),
                        pICMSST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_picmsst"].ToString()),
                        vICMSST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_vicmsst"].ToString()),
                        modBC = detBC,
                        modBCST = detBCST,
                        orig = origem

                    };
                    break;
                case Csticms.CstPart10:
                    break;
                case Csticms.Cst20:
                    icms = new ICMS20();
                    break;
                case Csticms.Cst30:
                    icms = new ICMS30()
                    {
                        CST = cs,
                        pMVAST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_pmvast"].ToString()),
                        pRedBCST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_predbcst"].ToString()),
                        vBCST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_vbcst"].ToString()),
                        pICMSST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_picmsst"].ToString()),
                        vICMSST = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["icms_vicmsst"].ToString()),
                        modBCST = detBCST,
                        orig = origem

                    };
                    break;
                case Csticms.Cst40:
                    icms = new ICMS40()
                    {
                        CST = cs,
                        orig = origem,
                    };
                    break;
                case Csticms.Cst41:
                    icms = new ICMS40()
                    {
                        CST = cs,
                        orig = origem,

                    };
                    break;
                case Csticms.CstRep41:
                    break;
                case Csticms.Cst50:
                    icms = new ICMS40()
                    {
                        CST = cs,
                        orig = origem,

                    };
                    break;
                case Csticms.Cst51:
                    break;
                case Csticms.Cst60:
                    break;
                case Csticms.Cst70:
                    break;
                case Csticms.Cst90:
                    break;
                case Csticms.CstPart90:
                    break;
                default:
                    break;
            }
            CSTPIS cstpis = new CSTPIS();
            switch (_nfa.Tables["nfe_item"].Rows[i]["pis_cst"].ToString())
            {
                case "01":
                    cstpis = CSTPIS.pis01;
                    break;
                case "02":
                    cstpis = CSTPIS.pis02;
                    break;
                case "06":
                    cstpis = CSTPIS.pis06;
                    break;
                case "08":
                    cstpis = CSTPIS.pis08;
                    break;
                case "49":
                    cstpis = CSTPIS.pis49;
                    break;
                case "50":
                    cstpis = CSTPIS.pis50;
                    break;
                case "54":
                    cstpis = CSTPIS.pis54;
                    break;
                case "55":
                    cstpis = CSTPIS.pis55;
                    break;
                default:
                    cstpis = (CSTPIS)Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["pis_cst"].ToString());
                    break;
            }

            PISBasico pistrib = new PISAliq();

            switch (cstpis)
            {
                case CSTPIS.pis01:
                    pistrib = new PISAliq()
                    {
                        CST = cstpis,
                        pPIS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_ppis"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vbc"].ToString()),
                        vPIS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                        //vAliqProd = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                    };
                    break;
                case CSTPIS.pis02:
                    pistrib = new PISAliq()
                    {
                        CST = cstpis,
                        pPIS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_ppis"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vbc"].ToString()),
                        vPIS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                        //vAliqProd = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                    };
                    break;
                case CSTPIS.pis03:
                    break;
                case CSTPIS.pis04:
                    break;
                case CSTPIS.pis05:
                    break;
                case CSTPIS.pis06:
                    pistrib = new PISNT()
                    {
                        CST = cstpis,
                        //vAliqProd = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                    };
                    break;
                case CSTPIS.pis07:
                    break;
                case CSTPIS.pis08:
                    pistrib = new PISNT()
                    {
                        CST = cstpis,
                        //vAliqProd = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                    };
                    break;
                case CSTPIS.pis09:
                    break;
                case CSTPIS.pis49:
                    pistrib = new PISOutr()
                    {
                        CST = cstpis,
                        pPIS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_ppis"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vbc"].ToString()),
                        vPIS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                        //vAliqProd = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                    };
                    break;
                case CSTPIS.pis50:
                    break;
                case CSTPIS.pis51:
                    break;
                case CSTPIS.pis52:
                    break;
                case CSTPIS.pis53:
                    break;
                case CSTPIS.pis54:
                    pistrib = new PISOutr()
                    {
                        CST = cstpis,
                        pPIS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_ppis"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vbc"].ToString()),
                        vPIS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                        //vAliqProd = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["pis_vpis"].ToString()),
                    };
                    break;
                case CSTPIS.pis55:
                    break;
                case CSTPIS.pis56:
                    break;
                case CSTPIS.pis60:
                    break;
                case CSTPIS.pis61:
                    break;
                case CSTPIS.pis62:
                    break;
                case CSTPIS.pis63:
                    break;
                case CSTPIS.pis64:
                    break;
                case CSTPIS.pis65:
                    break;
                case CSTPIS.pis66:
                    break;
                case CSTPIS.pis67:
                    break;
                case CSTPIS.pis70:
                    break;
                case CSTPIS.pis71:
                    break;
                case CSTPIS.pis72:
                    break;
                case CSTPIS.pis73:
                    break;
                case CSTPIS.pis74:
                    break;
                case CSTPIS.pis75:
                    break;
                case CSTPIS.pis98:
                    break;
                case CSTPIS.pis99:
                    break;
                default:
                    break;
            }

            //COFINS

            CSTCOFINS cstcofins = new CSTCOFINS();
            switch (_nfa.Tables["nfe_item"].Rows[i]["cofins_cst"].ToString())
            {
                case "01":
                    cstcofins = CSTCOFINS.cofins01;
                    break;
                case "02":
                    cstcofins = CSTCOFINS.cofins02;
                    break;
                case "06":
                    cstcofins = CSTCOFINS.cofins06;
                    break;
                case "08":
                    cstcofins = CSTCOFINS.cofins08;
                    break;
                case "49":
                    cstcofins = CSTCOFINS.cofins49;
                    break;
                case "50":
                    cstcofins = CSTCOFINS.cofins50;
                    break;
                case "54":
                    cstcofins = CSTCOFINS.cofins54;
                    break;

                default:
                    cstcofins = (CSTCOFINS)Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["cofins_cst"].ToString());
                    break;
            }

            COFINSBasico cofinstrib = new COFINSOutr();

            switch (cstcofins)
            {
                case CSTCOFINS.cofins01:
                    cofinstrib = new COFINSAliq()
                    {
                        CST = cstcofins,
                        pCOFINS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_pcofins"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_vbc"].ToString()),
                        vCOFINS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_vcofins"].ToString()),
                    };
                    break;
                case CSTCOFINS.cofins02:
                    cofinstrib = new COFINSAliq()
                    {
                        CST = cstcofins,
                        pCOFINS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_pcofins"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_vbc"].ToString()),
                        vCOFINS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_vcofins"].ToString()),
                    };
                    break;
                case CSTCOFINS.cofins03:
                    break;
                case CSTCOFINS.cofins04:
                    break;
                case CSTCOFINS.cofins05:
                    break;
                case CSTCOFINS.cofins06:
                    cofinstrib = new COFINSNT()
                    {
                        CST = cstcofins,
                    };
                    break;
                case CSTCOFINS.cofins07:
                    break;
                case CSTCOFINS.cofins08:
                    cofinstrib = new COFINSNT()
                    {
                        CST = cstcofins,
                    };
                    break;
                case CSTCOFINS.cofins09:
                    break;
                case CSTCOFINS.cofins49:
                    cofinstrib = new COFINSOutr()
                    {
                        CST = cstcofins,
                        pCOFINS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_pcofins"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_vbc"].ToString()),
                        vCOFINS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_vcofins"].ToString()),
                    };
                    break;
                case CSTCOFINS.cofins50:
                    break;
                case CSTCOFINS.cofins51:
                    break;
                case CSTCOFINS.cofins52:
                    break;
                case CSTCOFINS.cofins53:
                    break;
                case CSTCOFINS.cofins54:
                    cofinstrib = new COFINSOutr()
                    {
                        CST = cstcofins,
                        pCOFINS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_pcofins"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_vbc"].ToString()),
                        vCOFINS = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["cofins_vcofins"].ToString()),
                    };
                    break;
                case CSTCOFINS.cofins55:
                    break;
                case CSTCOFINS.cofins56:
                    break;
                case CSTCOFINS.cofins60:
                    break;
                case CSTCOFINS.cofins61:
                    break;
                case CSTCOFINS.cofins62:
                    break;
                case CSTCOFINS.cofins63:
                    break;
                case CSTCOFINS.cofins64:
                    break;
                case CSTCOFINS.cofins65:
                    break;
                case CSTCOFINS.cofins66:
                    break;
                case CSTCOFINS.cofins67:
                    break;
                case CSTCOFINS.cofins70:
                    break;
                case CSTCOFINS.cofins71:
                    break;
                case CSTCOFINS.cofins72:
                    break;
                case CSTCOFINS.cofins73:
                    break;
                case CSTCOFINS.cofins74:
                    break;
                case CSTCOFINS.cofins75:
                    break;
                case CSTCOFINS.cofins98:
                    break;
                case CSTCOFINS.cofins99:
                    break;
                default:
                    break;
            }

            IPIBasico ipitrib = new IPITrib();
            IPI ipi = new IPI()
            {
                //cEnq = 150,
                //qSelo = 0,
                //cSelo = "",
                TipoIPI = new IPITrib()
                {
                    CST = (CSTIPI.ipi99),// Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_cst"].ToString()),
                    pIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_pipi"].ToString()),
                    vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vbc"].ToString()),
                    vIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vipi"].ToString()),
                    //qUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_qunid"].ToString()),
                    //vUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vunid"].ToString())
                }
            };
            CSTIPI cstipi = new CSTIPI();

            switch (ipiString)
            {
                case "01":
                    cstipi = CSTIPI.ipi01;
                    break;
                case "02":
                    cstipi = CSTIPI.ipi02;
                    break;
                case "03":
                    cstipi = CSTIPI.ipi03;
                    break;
                case "04":
                    cstipi = CSTIPI.ipi04;
                    break;
                case "05":
                    cstipi = CSTIPI.ipi05;
                    break;
                case "50":
                    cstipi = CSTIPI.ipi50;
                    break;
                case "51":
                    cstipi = CSTIPI.ipi51;
                    break;
                case "52":
                    cstipi = CSTIPI.ipi52;
                    break;
                case "53":
                    cstipi = CSTIPI.ipi53;
                    break;
                case "54":
                    cstipi = CSTIPI.ipi54;
                    break;
                case "55":
                    cstipi = CSTIPI.ipi55;
                    break;
                default:
                    cstipi = CSTIPI.ipi99;
                    break;
            }

            switch (cstipi)
            {
                case CSTIPI.ipi00:
                    ipitrib = new IPITrib()
                    {
                        CST = cstipi,// Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_cst"].ToString()),
                        pIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_pipi"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vbc"].ToString()),
                        vIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vipi"].ToString()),
                        //qUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_qunid"].ToString()),
                        //vUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vunid"].ToString())
                    };
                    break;
                case CSTIPI.ipi49:
                    ipitrib = new IPITrib()
                    {
                        CST = cstipi,// Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_cst"].ToString()),
                        pIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_pipi"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vbc"].ToString()),
                        vIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vipi"].ToString()),
                        //qUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_qunid"].ToString()),
                        //vUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vunid"].ToString())
                    };
                    break;
                case CSTIPI.ipi50:
                    ipitrib = new IPITrib()
                    {
                        CST = cstipi,// Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_cst"].ToString()),
                        pIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_pipi"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vbc"].ToString()),
                        vIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vipi"].ToString()),
                        //qUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_qunid"].ToString()),
                        //vUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vunid"].ToString())
                    };
                    break;
                case CSTIPI.ipi99:
                    ipitrib = new IPITrib()
                    {
                        CST = cstipi,// Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_cst"].ToString()),
                        pIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_pipi"].ToString()),
                        vBC = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vbc"].ToString()),
                        vIPI = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vipi"].ToString()),
                        //qUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_qunid"].ToString()),
                        //vUnid = Convert.ToDecimal(_nfa.Tables["nfe_item"].Rows[i]["ipitrib_vunid"].ToString())
                    };
                    break;
                case CSTIPI.ipi01:
                    ipitrib = new IPINT()
                    {
                        CST = cstipi,
                    };
                    break;
                case CSTIPI.ipi02:
                    ipitrib = new IPINT()
                    {
                        CST = cstipi,
                    };
                    break;
                case CSTIPI.ipi03:
                    ipitrib = new IPINT()
                    {
                        CST = cstipi,
                    };
                    break;
                case CSTIPI.ipi04:
                    ipitrib = new IPINT()
                    {
                        CST = cstipi,
                    };
                    break;
                case CSTIPI.ipi05:
                    break;
                case CSTIPI.ipi51:
                    ipitrib = new IPINT()
                    {
                        CST = cstipi,
                    };
                    break;
                case CSTIPI.ipi52:
                    ipitrib = new IPINT()
                    {
                        CST = cstipi,
                    };
                    break;
                case CSTIPI.ipi53:
                    ipitrib = new IPINT()
                    {
                        CST = cstipi,
                    };
                    break;
                case CSTIPI.ipi54:
                    ipitrib = new IPINT()
                    {
                        CST = cstipi,
                    };
                    break;
                case CSTIPI.ipi55:
                    ipitrib = new IPINT()
                    {
                        CST = cstipi,
                    };
                    break;
                default:
                    break;
            }


            imposto imp = new imposto();

            //detalhe.impostoDevol = new NFe.Classes.Informacoes.Detalhe.impostoDevol() { };
            //detalhe.infAdProd = "";

            if (imp_import != null)
            {
                imp.II = imp_import;
            }

            imp.ICMS = new ICMS() { TipoICMS = icms };
            IPI ipitot = new IPI();
            ipitot.TipoIPI = ipitrib;

            if (!string.IsNullOrEmpty(_nfa.Tables["nfe_item"].Rows[i]["ipi_cenq"].ToString()))
                ipitot.cEnq = Convert.ToInt32(_nfa.Tables["nfe_item"].Rows[i]["ipi_cenq"].ToString());
            else
                ipitot.cEnq = 999;

            imp.IPI = ipitot;
            imp.PIS = new PIS() { TipoPIS = pistrib };
            imp.COFINS = new COFINS() { TipoCOFINS = cofinstrib };
            detalhe.imposto = imp;

            return detalhe;
        }

        protected virtual ICMSBasico InformarICMS(Csticms CST, VersaoServico versao)
        {
            var icms20 = new ICMS20
            {
                orig = OrigemMercadoria.OmNacional,
                CST = Csticms.Cst20,
                modBC = DeterminacaoBaseIcms.DbiValorOperacao,
                vBC = 1,
                pICMS = 17,
                vICMS = 0.17m,
                motDesICMS = MotivoDesoneracaoIcms.MdiTaxi
            };
            if (versao == VersaoServico.ve310)
                icms20.vICMSDeson = 0.10m; //V3.00 ou maior Somente

            switch (CST)
            {
                case Csticms.Cst00:
                    return new ICMS00
                    {
                        CST = Csticms.Cst00,
                        modBC = DeterminacaoBaseIcms.DbiValorOperacao,
                        orig = OrigemMercadoria.OmNacional,
                        pICMS = 17,
                        vBC = 1,
                        vICMS = 0.17m
                    };
                case Csticms.Cst20:
                    return icms20;
                    //Outros casos aqui
            }

            return new ICMS10();
        }

        protected virtual ICMSBasico ObterIcmsBasico(CRT crt)
        {
            //Leia os dados de seu banco de dados e em seguida alimente o objeto ICMSGeral, como no exemplo abaixo.
            var icmsGeral = new ICMSGeral
            {
                orig = OrigemMercadoria.OmNacional,
                CST = Csticms.Cst20,
                modBC = DeterminacaoBaseIcms.DbiValorOperacao,
                vBC = 1,
                pICMS = 17,
                vICMS = 0.17m,
                motDesICMS = MotivoDesoneracaoIcms.MdiTaxi
            };
            return icmsGeral.ObterICMSBasico(crt);
        }

        protected virtual ICMSBasico InformarCSOSN(Csosnicms CST)
        {
            switch (CST)
            {
                case Csosnicms.Csosn101:
                    return new ICMSSN101
                    {
                        CSOSN = Csosnicms.Csosn101,
                        orig = OrigemMercadoria.OmNacional
                    };
                case Csosnicms.Csosn102:
                    return new ICMSSN102
                    {
                        CSOSN = Csosnicms.Csosn102,
                        orig = OrigemMercadoria.OmNacional
                    };
                //Outros casos aqui
                default:
                    return new ICMSSN201();
            }
        }

        protected virtual total GetTotal(VersaoServico versao, List<det> produtos)
        {
            ICMSTot total = new ICMSTot()
            {
                vBC = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vbc"].ToString()),
                vICMS = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vicms"].ToString()),
                vBCST = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vbcst"].ToString()),
                vST = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_st"].ToString()),
                vProd = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vprod"].ToString()),
                vFrete = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vfrete"].ToString()),
                vSeg = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vseg"].ToString()),
                vDesc = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vdesc"].ToString()),
                vII = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vii"].ToString()),
                vIPI = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vipi"].ToString()),
                vPIS = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vpis"].ToString()),
                vCOFINS = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vcofins"].ToString()),
                vOutro = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_voutro"].ToString()),
                vNF = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["icmstot_vnf"].ToString()),
            };


            if (versao == VersaoServico.ve310)
                total.vICMSDeson = 0;

            var t = new total { ICMSTot = total };
            return t;

        }

        protected virtual transp GetTransporte()
        {
            var volumes = new List<vol> { GetVolume() };

            var t = new transp
            {
                modFrete = (ModalidadeFrete)Convert.ToInt32(_nfa.Tables["nfe_cab"].Rows[0]["transp_modfrete"].ToString()),
                vol = volumes,


            };

            transporta tra = new transporta();

            tra.CNPJ = _nfa.Tables["nfe_cab"].Rows[0]["transporta_cnpj"].ToString() == "" ? null : _nfa.Tables["nfe_cab"].Rows[0]["transporta_cnpj"].ToString();
            tra.IE = _nfa.Tables["nfe_cab"].Rows[0]["transporta_ie"].ToString() == "" ? null : _nfa.Tables["nfe_cab"].Rows[0]["transporta_ie"].ToString();
            tra.xNome = _nfa.Tables["nfe_cab"].Rows[0]["transporta_xnome"].ToString() == "" ? null : _nfa.Tables["nfe_cab"].Rows[0]["transporta_xnome"].ToString();
            tra.xEnder = _nfa.Tables["nfe_cab"].Rows[0]["transporta_xender"].ToString() == "" ? null : _nfa.Tables["nfe_cab"].Rows[0]["transporta_xender"].ToString();
            tra.xMun = _nfa.Tables["nfe_cab"].Rows[0]["transporta_xmun"].ToString() == "" ? null : _nfa.Tables["nfe_cab"].Rows[0]["transporta_xmun"].ToString();
            tra.UF = _nfa.Tables["nfe_cab"].Rows[0]["transporta_uf"].ToString() == "" ? null : _nfa.Tables["nfe_cab"].Rows[0]["transporta_uf"].ToString();

            t.transporta = tra;

            return t;
        }

        protected virtual vol GetVolume()
        {
            var v = new vol
            {
                esp = _nfa.Tables["nfe_cab"].Rows[0]["vol_esp"].ToString(),
                //marca = _nfa.Tables["nfe_cab"].Rows[0]["vol_marca"].ToString(),
                //nVol = _nfa.Tables["nfe_cab"].Rows[0]["vol_nvol"].ToString(),
                pesoL = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["vol_pesol"].ToString()),
                pesoB = Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["vol_pesob"].ToString()),
                qVol = Convert.ToInt32(Convert.ToDecimal(_nfa.Tables["nfe_cab"].Rows[0]["vol_qvol"].ToString())),

            };

            return v;
        }

        protected virtual cobr GetCobranca(ICMSTot icmsTot)
        {
            cobr cob = new cobr();
            fat f = new fat();
            f.vOrig = icmsTot.vNF;
            cob.dup = new List<dup>();

            cob.fat = f;

            if (_nfa.Tables["nfe_dupli"].Rows.Count > 0)
            {
                foreach (DataRow r in _nfa.Tables["nfe_dupli"].Rows)
                {
                    dup d = new dup()
                    {
                        dVenc = Convert.ToDateTime(r["dvenc"]).ToString("yyyy-MM-dd"),
                        nDup = r["ndup"].ToString(),
                        vDup = Convert.ToDecimal(r["vdup"].ToString())
                    };

                    cob.dup.Add(d);
                }

                return cob;

            }
            else
                return null;



        }

        protected virtual List<pag> GetPagamento(ICMSTot icmsTot)
        {
            var valorPagto = Valor.Arredondar(icmsTot.vProd / 2, 2);
            var p = new List<pag>
            {
                new pag {tPag = FormaPagamento.fpDinheiro, vPag = valorPagto},
                new pag {tPag = FormaPagamento.fpCheque, vPag = icmsTot.vProd - valorPagto}
            };
            return p;
        }



        private void TrataRetorno(RetornoBasico retornoBasico)
        {
            richTextBox1.Clear();
            //webBrowser1.


            EnvioStr(richTextBox1, retornoBasico.EnvioStr);
            RetornoStr(richTextBox1, retornoBasico.RetornoStr);
            RetornoXml(webBrowser1, retornoBasico.RetornoStr);
            RetornoCompletoStr(richTextBox1, retornoBasico.RetornoCompletoStr);
            RetornoDados(retornoBasico.Retorno, richTextBox1);
        }
        #region Tratamento de retornos dos Serviços

        internal void RetornoDados<T>(T objeto, RichTextBox richTextBox) where T : class
        {
            richTextBox.Clear();

            foreach (var atributos in objeto.LerPropriedades())
            {
                richTextBox.AppendText(atributos.Key + " = " + atributos.Value + "\r");
            }
        }

        internal void RetornoCompletoStr(RichTextBox richTextBox, string retornoCompletoStr)
        {
            richTextBox.Clear();
            richTextBox.AppendText(retornoCompletoStr);
        }

        internal void EnvioStr(RichTextBox richTextBox, string envioStr)
        {
            richTextBox.Clear();
            richTextBox.AppendText(envioStr);
        }

        internal void RetornoXml(WebBrowser webBrowser, string retornoXmlString)
        {
            var stw = new StreamWriter(_path + @"\tmp.xml");
            stw.WriteLine(retornoXmlString);
            stw.Close();
            webBrowser.Navigate(_path + @"\tmp.xml");
        }

        internal void RetornoStr(RichTextBox richTextBox, string retornoXmlString)
        {
            richTextBox.Clear();
            richTextBox.AppendText(retornoXmlString);
        }

        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                #region Cancelar NFe

                var idlote = "64877";
                _nfa = new NFeBLL().GetNfaDataTable(textBox2.Text);

                var sequenciaEvento = 1;// _nfa.Tables["nfe_cab"].Rows[0]["cab_serial"].ToString();

                var protocolo = _nfa.Tables["nfe_cab"].Rows[0]["nprot"].ToString();

                var chave = _nfa.Tables["nfe_cab"].Rows[0]["chnfe"].ToString();

                var justificativa = "ERRO INTERNO DE SISTEMA....";

                var servicoNFe = new ServicosNFe(_configuracoes.CfgServico);
                var cpfcnpj = string.IsNullOrEmpty(_configuracoes.Emitente.CNPJ)
                    ? _configuracoes.Emitente.CPF
                    : _configuracoes.Emitente.CNPJ;
                var retornoCancelamento = servicoNFe.RecepcaoEventoCancelamento(Convert.ToInt32(idlote),
                    Convert.ToInt32(sequenciaEvento), protocolo, chave, justificativa, cpfcnpj);
                TrataRetorno(retornoCancelamento);

                richTextBox1.Text = retornoCancelamento.RetornoCompletoStr;

                #endregion
            }
            catch (Exception ex)
            {

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                #region Consulta Recibo de lote

                _nfa = new NFeBLL().GetNfaDataTable(textBox3.Text);

                var recibo = _nfa.Tables["nfe_cab"].Rows[0]["nrec"].ToString();
                if (string.IsNullOrEmpty(recibo)) throw new Exception("O número do recibo deve ser informado!");
                var servicoNFe = new ServicosNFe(_configuracoes.CfgServico);
                var retornoRecibo = servicoNFe.NFeRetAutorizacao(recibo);

                TrataRetorno(retornoRecibo);

                #endregion
            }
            catch (Exception ex)
            {

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                #region Consulta Situação NFe
                _nfa = new NFeBLL().GetNfaDataTable(textBox3.Text);
                var chave = _nfa.Tables["nfe_cab"].Rows[0]["chnfe"].ToString();
                if (string.IsNullOrEmpty(chave)) throw new Exception("A Chave deve ser informada!");
                if (chave.Length != 44) throw new Exception("Chave deve conter 44 caracteres!");

                var servicoNFe = new ServicosNFe(_configuracoes.CfgServico);
                var retornoConsulta = servicoNFe.NfeConsultaProtocolo(chave);
                TrataRetorno(retornoConsulta);

                #endregion
            }
            catch (Exception ex)
            {

            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _nfa = new NFeBLL().GetNfaDataTable(textBox1.Text);
            Imprimir(_nfa.Tables["nfe_cab"].Rows[0]["xml"].ToString());
        }

        private void Imprimir(string xml)
        {
            try
            {
                #region Carrega um XML com nfeProc para a variável

                var arquivoXml = xml;// Funcoes.BuscarArquivoXml();
                if (string.IsNullOrEmpty(arquivoXml))
                    return;
                var proc = new nfeProc().CarregarDeXmlString(arquivoXml);
                if (proc.NFe.infNFe.ide.mod != ModeloDocumento.NFe)
                    throw new Exception("O XML informado não é um NFe!");

                #endregion

                #region Abre a visualização do relatório para impressão
                var danfe = new DanfeFrNfe(proc, new ConfiguracaoDanfeNfe());
                //danfe.Visualizar();
                danfe.Imprimir(false, "");
                //danfe.ExibirDesign();
                //danfe.ExportarPdf(@"d:\teste.pdf");

                #endregion

            }
            catch (Exception ex)
            {

            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //Exemplo com using para chamar o método Dispose da classe.
            //Usar dessa forma, especialmente, quando for usar certificado A3 com a senha salva.
            using (var servicoNFe = new ServicosNFe(_configuracoes.CfgServico))
            {
                var retornoStatus = servicoNFe.NfeStatusServico();
                TrataRetorno(retornoStatus);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            EnviaNFe();
        }
        void EnviaNFe()
        {

            NFeBLL nBll = new NFeBLL();
            DataTable dt = nBll.GetListNfeToSend();
            //protNFe protocolo = null;
            if (dt.Rows.Count == 0) return;

            foreach (DataRow dr in dt.Rows)
            {

                switch (dr["cstat"].ToString())
                {
                    case "1":
                        string xml = "";
                        protNFe protocolo = EnviaNFeSilent(dr["ide_nnf"].ToString(), out xml);

                        if (protocolo == null)
                        {
                            nBll.SalvaErroNFe(_lastCstat, _lastXmot, dr["ide_nnf"].ToString());
                            break;
                        }


                        nBll.SalvaRetornoNFe(protocolo.infProt.chNFe, protocolo.infProt.dhRecbto, protocolo.infProt.digVal, protocolo.infProt.cStat.ToString(),
                            protocolo.infProt.xMotivo, protocolo.infProt.nProt, protocolo.infProt.nProt, xml.Replace("\"", "\\\""), dr["ide_nnf"].ToString());

                        if (protocolo.infProt.cStat.ToString() == "100")
                        {
                            Imprimir(xml);
                            Imprimir(xml);
                        }
                        break;
                    case "2":
                        try
                        {
                            #region Cancelar NFe

                            var idlote = "64877";
                            _nfa = new NFeBLL().GetNfaDataTable(dr["ide_nnf"].ToString());

                            var sequenciaEvento = 1;// _nfa.Tables["nfe_cab"].Rows[0]["cab_serial"].ToString();

                            var srtprotocolo = _nfa.Tables["nfe_cab"].Rows[0]["nprot"].ToString();

                            var chave = _nfa.Tables["nfe_cab"].Rows[0]["chnfe"].ToString();

                            var justificativa = "ERRO INTERNO DE SISTEMA....";

                            var servicoNFe = new ServicosNFe(_configuracoes.CfgServico);
                            var cpfcnpj = string.IsNullOrEmpty(_configuracoes.Emitente.CNPJ)
                                ? _configuracoes.Emitente.CPF
                                : _configuracoes.Emitente.CNPJ;
                            var retornoCancelamento = servicoNFe.RecepcaoEventoCancelamento(Convert.ToInt32(idlote),
                                Convert.ToInt32(sequenciaEvento), srtprotocolo, chave, justificativa, cpfcnpj);
                           

                            richTextBox1.Text = retornoCancelamento.RetornoCompletoStr;

                            System.Threading.Thread.Sleep(2000);

                            var retornoConsulta = servicoNFe.NfeConsultaProtocolo(chave);
                            TrataRetorno(retornoConsulta);

                            nBll.SalvaErroNFe(retornoConsulta.Retorno.cStat.ToString(), retornoConsulta.Retorno.xMotivo, dr["ide_nnf"].ToString());

                            #endregion
                        }
                        catch (Exception ex)
                        {

                        }
                        break;
                    case "3":
                        break;
                    case "5":
                        try
                        {
                            #region Carta de correção
                            var idlote = "64877";
                            _nfa = new NFeBLL().GetNfaDataTable(dr["ide_nnf"].ToString());
                            var sequenciaEvento = _nfa.Tables["nfe_cab"].Rows[0]["nfa_cce_id"].ToString();
                            if (string.IsNullOrEmpty(sequenciaEvento))
                                throw new Exception("O número sequencial deve ser informado!");

                            var chave = _nfa.Tables["nfe_cab"].Rows[0]["chnfe"].ToString();

                            var correcao = _nfa.Tables["nfe_cab"].Rows[0]["nfa_cce"].ToString().Replace("\n","").Replace("\r","");

                            var servicoNFe = new ServicosNFe(_configuracoes.CfgServico);
                            var cpfcnpj = string.IsNullOrEmpty(_configuracoes.Emitente.CNPJ)
                                ? _configuracoes.Emitente.CPF
                                : _configuracoes.Emitente.CNPJ;
                            var retornoCartaCorrecao = servicoNFe.RecepcaoEventoCartaCorrecao(Convert.ToInt32(idlote),
                                Convert.ToInt16(sequenciaEvento), chave, correcao, cpfcnpj);

                            System.Threading.Thread.Sleep(2000);

                            var retornoConsulta = servicoNFe.NfeConsultaProtocolo(chave);

                            nBll.SalvaErroNFe(retornoConsulta.Retorno.cStat.ToString(), retornoConsulta.Retorno.xMotivo, dr["ide_nnf"].ToString());

                            #endregion
                        }
                        catch (Exception ex)
                        {
                            richTextBox1.Text += "\n" + ex.Message;
                        }
                        break;

                    default:
                        break;
                }

                
            }

        }
        private void ImprimeDireto(string p_pathFilePdf)
        {

            try
            {

                FileInfo file = new FileInfo(p_pathFilePdf);
                if (file.Exists)
                {

                    Process process = new Process();
                    Process objP = new Process();

                    objP.StartInfo.FileName = p_pathFilePdf;

                    objP.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; //Hide the window.
                    objP.StartInfo.Verb = "print";
                    objP.StartInfo.CreateNoWindow = true;
                    objP.Start();

                    objP.CloseMainWindow();
                }
                else
                {

                    //LogProcesso("OK", "MandaImprimirPS()", "Arquivo não existe.");

                }

            }
            catch (Exception ex)
            {

                //LogProcesso("Erro", ex.Source, ex.Message);

            }

        }

        private void button8_Click(object sender, EventArgs e)
        {
            timer1.Enabled = !timer1.Enabled;
            pictureBox1.Image = imageList1.Images[Convert.ToInt32(!timer1.Enabled)];
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            EnviaNFe();
            timer1.Start();
        }
    }
}
